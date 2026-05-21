namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private Control BuildTenantDocumentsList(IReadOnlyList<OwnedDocumentAttachment> documents)
    {
        if (documents.Count == 0)
        {
            return new Label
            {
                AutoSize = true,
                Text = "No documents uploaded.",
                ForeColor = Color.DimGray
            };
        }

        var rows = documents.Select(DocumentAttachmentRow.FromDocument).ToArray();
        var grid = new DataGridView
        {
            Width = 460,
            Height = 130,
            ReadOnly = true,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            DataSource = rows
        };
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "DocumentName",
            HeaderText = "Document",
            DataPropertyName = nameof(DocumentAttachmentRow.Name),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "FilePath",
            HeaderText = "File Path",
            DataPropertyName = nameof(DocumentAttachmentRow.FilePath),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Status",
            HeaderText = "Status",
            DataPropertyName = nameof(DocumentAttachmentRow.Status),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        });
        grid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "CopyPath",
            HeaderText = string.Empty,
            Text = "Copy Path",
            UseColumnTextForButtonValue = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        });
        grid.CellFormatting += OnTenantDocumentsGridCellFormatting;
        grid.CellContentClick += OnTenantDocumentsGridCellContentClick;
        return grid;
    }

    private void OnTenantDocumentsGridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (sender is not DataGridView grid ||
            e.RowIndex < 0 ||
            grid.Columns[e.ColumnIndex].Name != "CopyPath" ||
            grid.Rows[e.RowIndex].DataBoundItem is not DocumentAttachmentRow row ||
            row.CanCopyPath)
        {
            return;
        }

        e.Value = "No Path";
    }

    private void OnTenantDocumentsGridCellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (sender is not DataGridView grid ||
            e.RowIndex < 0 ||
            e.ColumnIndex < 0 ||
            grid.Columns[e.ColumnIndex].Name != "CopyPath" ||
            grid.Rows[e.RowIndex].DataBoundItem is not DocumentAttachmentRow row ||
            !row.CanCopyPath)
        {
            return;
        }

        Clipboard.SetText(row.FilePath);
        ShowTenantDocumentsStatus(grid, "File path copied.");
    }

    private static void ShowTenantDocumentsStatus(Control grid, string message)
    {
        if (grid.Parent is not TableLayoutPanel parent)
        {
            return;
        }

        var status = parent.Controls
            .OfType<Label>()
            .FirstOrDefault(label => label.Name == "TenantDocumentsStatus");
        if (status is null)
        {
            status = new Label
            {
                Name = "TenantDocumentsStatus",
                AutoSize = true,
                ForeColor = Color.DarkGreen
            };
            parent.Controls.Add(status);
        }

        status.Text = message;
    }
}
