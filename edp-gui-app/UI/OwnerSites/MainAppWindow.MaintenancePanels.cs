namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private (Control Panel, DataGridView Grid, Label Status) BuildMaintenancePanel()
    {
        var headingText = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };
        headingText.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Maintenance Requests",
            Font = new Font(Font.FontFamily, 20, FontStyle.Bold)
        });
        headingText.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(560, 0),
            Text = "Create and track maintenance tickets for your sites."
        });

        var add = BuildMaintenanceButton("Add Request", OnAddMaintenanceClicked);
        var refresh = BuildMaintenanceButton("Refresh", OnRefreshMaintenanceClicked);
        var back = BuildMaintenanceButton("Back to Sites", (_, _) => ShowOwnerSitesView());

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        actions.Controls.Add(add);
        actions.Controls.Add(refresh);
        actions.Controls.Add(back);

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2 };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(headingText, 0, 0);
        header.Controls.Add(actions, 1, 0);

        var status = new Label { AutoSize = true, MaximumSize = new Size(720, 0), Margin = new Padding(0, 12, 0, 12) };
        var grid = BuildMaintenanceGrid();

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.White,
            Padding = new Padding(26)
        };
        panel.RowStyles.Add(new RowStyle());
        panel.RowStyles.Add(new RowStyle());
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(header, 0, 0);
        panel.Controls.Add(status, 0, 1);
        panel.Controls.Add(grid, 0, 2);

        return (panel, grid, status);
    }

    private static Button BuildMaintenanceButton(string text, EventHandler onClick)
    {
        var button = new Button { AutoSize = true, Text = text, Padding = new Padding(10, 6, 10, 6) };
        button.Click += onClick;
        return button;
    }

    private DataGridView BuildMaintenanceGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
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
            DataSource = _maintenanceBindingSource
        };
        AddMaintenanceColumn(grid, nameof(OwnerMaintenanceRequest.TicketId), "Ticket ID", 90);
        AddMaintenanceColumn(grid, nameof(OwnerMaintenanceRequest.SiteName), "Site", 150);
        AddMaintenanceColumn(grid, nameof(OwnerMaintenanceRequest.Title), "Title", 180);
        AddMaintenanceColumn(grid, nameof(OwnerMaintenanceRequest.Description), "Description", 240);
        AddMaintenanceColumn(grid, nameof(OwnerMaintenanceRequest.Priority), "Priority", 100);
        AddMaintenanceColumn(grid, nameof(OwnerMaintenanceRequest.Status), "Status", 110);
        AddMaintenanceColumn(grid, nameof(OwnerMaintenanceRequest.RequestedAt), "Date Requested", 150, "yyyy-MM-dd HH:mm");
        AddMaintenanceColumn(grid, nameof(OwnerMaintenanceRequest.ResolvedAt), "Date Resolved", 150, "yyyy-MM-dd HH:mm");
        AddMaintenanceColumn(grid, nameof(OwnerMaintenanceRequest.Notes), "Notes", 260);
        return grid;
    }

    private static void AddMaintenanceColumn(
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
            Width = width,
            ReadOnly = true
        };

        if (!string.IsNullOrWhiteSpace(format))
        {
            column.DefaultCellStyle.Format = format;
        }

        grid.Columns.Add(column);
    }
}
