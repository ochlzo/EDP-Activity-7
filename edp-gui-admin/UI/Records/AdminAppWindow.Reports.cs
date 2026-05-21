namespace edp_gui_admin;

public sealed partial class AdminAppWindow
{
    private TabPage BuildReportsTab()
    {
        _reportSelector = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 220,
            DataSource = AdminReportCatalog.ReportNames.ToArray()
        };
        _reportSelector.SelectedIndexChanged += (_, _) => ApplyReportSelection();

        _reportSignatoryTextBox = new TextBox
        {
            Width = 240,
            PlaceholderText = "Type signatory name"
        };

        _reportStatusLabel = new Label
        {
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(10, 6, 0, 0)
        };

        var export = BuildActionButton("Export to Excel", async (_, _) => await ExportSelectedReportAsync());
        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 8)
        };
        actions.Controls.Add(new Label { AutoSize = true, Text = "Report", Margin = new Padding(0, 6, 8, 0) });
        actions.Controls.Add(_reportSelector);
        actions.Controls.Add(new Label { AutoSize = true, Text = "Signatory", Margin = new Padding(12, 6, 8, 0) });
        actions.Controls.Add(_reportSignatoryTextBox);
        actions.Controls.Add(export);
        actions.Controls.Add(_reportStatusLabel);

        var grid = BuildRecordGrid(_reportSource);
        grid.ReadOnly = true;
        grid.AutoGenerateColumns = false;
        ConfigureReportGrid(grid);

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        panel.RowStyles.Add(new RowStyle());
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(actions, 0, 0);
        panel.Controls.Add(grid, 0, 1);

        return new TabPage(ReportsTab) { Controls = { panel } };
    }

    private static void ConfigureReportGrid(DataGridView grid)
    {
        AddReportColumn(grid, nameof(AdminReportRow.Category), "Category", 130);
        AddReportColumn(grid, nameof(AdminReportRow.ParentName), "Parent", 180);
        AddReportColumn(grid, nameof(AdminReportRow.ItemName), "Item", 220);
        AddReportColumn(grid, nameof(AdminReportRow.Status), "Status", 120);
        AddReportColumn(grid, nameof(AdminReportRow.Date), "Date", 150, "yyyy-MM-dd HH:mm");
        AddReportColumn(grid, nameof(AdminReportRow.Notes), "Notes", 320);
    }

    private static void AddReportColumn(
        DataGridView grid,
        string propertyName,
        string headerText,
        int width,
        string? format = null)
    {
        var column = new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            HeaderText = headerText,
            ReadOnly = true,
            Width = width
        };

        if (!string.IsNullOrWhiteSpace(format))
        {
            column.DefaultCellStyle.Format = format;
        }

        grid.Columns.Add(column);
    }

    private void ApplyReportSelection()
    {
        if (_reportSelector is null || _reportStatusLabel is null)
        {
            return;
        }

        var reportName = Convert.ToString(_reportSelector.SelectedItem) ?? AdminReportCatalog.ReportNames[0];
        var rows = AdminReportCatalog.FilterRows(reportName, _allReportRows);
        _reportSource.DataSource = rows;
        _reportStatusLabel.Text = $"{rows.Count} row(s)";
    }

    private async Task ExportSelectedReportAsync()
    {
        try
        {
            var reportName = Convert.ToString(_reportSelector.SelectedItem) ?? string.Empty;
            var signatory = _reportSignatoryTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(signatory))
            {
                _reportStatusLabel.ForeColor = Color.Firebrick;
                _reportStatusLabel.Text = "Type the signatory before export.";
                return;
            }

            var rows = AdminReportCatalog.FilterRows(reportName, _allReportRows);
            using var dialog = new SaveFileDialog
            {
                Title = "Export Excel Report",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"{reportName.Replace(' ', '-')}-report.xlsx",
                AddExtension = true,
                DefaultExt = "xlsx"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            await Task.Run(() => AdminExcelReportExporter.Export(reportName, rows, signatory, dialog.FileName));
            _reportStatusLabel.ForeColor = Color.ForestGreen;
            _reportStatusLabel.Text = $"Exported {rows.Count} row(s).";
        }
        catch (Exception ex)
        {
            _reportStatusLabel.ForeColor = Color.Firebrick;
            _reportStatusLabel.Text = ex.Message;
        }
    }
}
