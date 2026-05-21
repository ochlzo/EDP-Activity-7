namespace edp_gui_admin;

public sealed partial class AdminAppWindow
{
    private TabPage BuildOccupancyHistoryTab(TextBox search)
    {
        var grid = BuildRecordGrid(_occupancySource);
        grid.ReadOnly = true;
        grid.AutoGenerateColumns = false;
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(AdminOccupancyHistoryRow.HistoryId),
            HeaderText = "History ID",
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(AdminOccupancyHistoryRow.TenantName),
            HeaderText = "Tenant Name",
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(AdminOccupancyHistoryRow.TenantId),
            HeaderText = "Tenant ID",
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(AdminOccupancyHistoryRow.RoomName),
            HeaderText = "Room Name",
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(AdminOccupancyHistoryRow.RoomId),
            HeaderText = "Room ID",
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(AdminOccupancyHistoryRow.DateOccupied),
            HeaderText = "Date Occupied",
            ReadOnly = true,
            DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm" },
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(AdminOccupancyHistoryRow.Notes),
            HeaderText = "Notes",
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 8)
        };
        AddParentFilterControls(OccupancyTab, actions);
        actions.Controls.Add(BuildSearchPanel(search));

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        panel.RowStyles.Add(new RowStyle());
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(actions, 0, 0);
        panel.Controls.Add(grid, 0, 1);

        return new TabPage(OccupancyTab) { Controls = { panel } };
    }
}
