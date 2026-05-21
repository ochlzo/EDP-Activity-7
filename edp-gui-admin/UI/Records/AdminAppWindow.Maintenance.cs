namespace edp_gui_admin;

public sealed partial class AdminAppWindow
{
    private TabPage BuildMaintenanceTab(TextBox search)
    {
        var grid = BuildRecordGrid(_maintenanceSource);
        grid.ReadOnly = true;
        ConfigureMaintenanceGrid(grid);

        var actions = BuildMaintenanceActions();
        actions.Controls.Add(BuildSearchPanel(search));

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        panel.RowStyles.Add(new RowStyle());
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(actions, 0, 0);
        panel.Controls.Add(grid, 0, 1);

        return new TabPage(MaintenanceTab) { Controls = { panel } };
    }

    private FlowLayoutPanel BuildMaintenanceActions()
    {
        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 8)
        };
        actions.Controls.Add(BuildActionButton("Resolve", async (_, _) => await ResolveCurrentAsync()));
        actions.Controls.Add(BuildActionButton("Edit", async (_, _) => await EditCurrentAsync()));
        actions.Controls.Add(BuildActionButton("Delete", async (_, _) => await DeleteCurrentAsync()));
        return actions;
    }

    private static void ConfigureMaintenanceGrid(DataGridView grid)
    {
        AddMaintenanceColumn(grid, nameof(AdminMaintenanceTicket.TicketId), "Ticket ID", 90);
        AddMaintenanceColumn(grid, nameof(AdminMaintenanceTicket.SiteName), "Site", 150);
        AddMaintenanceColumn(grid, nameof(AdminMaintenanceTicket.Title), "Title", 220);
        AddMaintenanceColumn(grid, nameof(AdminMaintenanceTicket.Description), "Description", 260);
        AddMaintenanceColumn(grid, nameof(AdminMaintenanceTicket.Priority), "Priority", 100);
        AddMaintenanceColumn(grid, nameof(AdminMaintenanceTicket.Status), "Status", 110);
        AddMaintenanceColumn(grid, nameof(AdminMaintenanceTicket.RequestedAt), "Date Requested", 150, "yyyy-MM-dd HH:mm");
        AddMaintenanceColumn(grid, nameof(AdminMaintenanceTicket.ResolvedAt), "Date Resolved", 150, "yyyy-MM-dd HH:mm");
        AddMaintenanceColumn(grid, nameof(AdminMaintenanceTicket.Notes), "Notes", 260);
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
            ReadOnly = true,
            Width = width
        };

        if (!string.IsNullOrWhiteSpace(format))
        {
            column.DefaultCellStyle.Format = format;
        }

        grid.Columns.Add(column);
    }

    private async Task ResolveMaintenanceTicketAsync(AdminMaintenanceTicket ticket)
    {
        var values = ShowRecordDialog(
            "Resolve Maintenance Ticket",
            DialogField.Text("notes", "Notes", ticket.Notes));
        if (values is not null)
        {
            await _transactionService.ResolveMaintenanceTicketAsync(ticket.TicketId, values["notes"], "admin");
        }
    }

    private async Task EditMaintenanceTicketAsync(AdminMaintenanceTicket ticket)
    {
        var priorityOptions = BuildMaintenancePriorityOptions(ticket.Priority);
        var statusOptions = BuildMaintenanceStatusOptions(ticket.Status);
        var values = ShowRecordDialog(
            "Edit Maintenance Ticket",
            DialogField.Text("title", "Title", ticket.Title),
            DialogField.Combo("priority", "Priority", ticket.Priority, priorityOptions),
            DialogField.Combo("status", "Status", ticket.Status, statusOptions),
            DialogField.Text("notes", "Notes", ticket.Notes));
        if (values is not null)
        {
            await _transactionService.UpdateMaintenanceTicketAsync(
                ticket.TicketId,
                values["title"],
                values["priority"],
                values["status"],
                values["notes"],
                "admin");
        }
    }

    private async Task DeleteMaintenanceTicketAsync(AdminMaintenanceTicket ticket)
    {
        await _transactionService.DeleteMaintenanceTicketAsync(ticket.TicketId, "admin");
    }

    private static IReadOnlyList<DialogOption> BuildMaintenancePriorityOptions(string currentValue)
    {
        return BuildMaintenanceOptions(["Low", "Normal", "High", "Urgent"], currentValue);
    }

    private static IReadOnlyList<DialogOption> BuildMaintenanceStatusOptions(string currentValue)
    {
        return BuildMaintenanceOptions(["Open", "In Progress", "Resolved"], currentValue);
    }

    private static IReadOnlyList<DialogOption> BuildMaintenanceOptions(
        IReadOnlyList<string> values,
        string currentValue)
    {
        var options = values.Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(value => new DialogOption(value, value))
            .ToList();

        if (!options.Any(option => string.Equals(option.Value, currentValue, StringComparison.OrdinalIgnoreCase)))
        {
            options.Insert(0, new DialogOption(currentValue, currentValue));
        }

        return options;
    }
}
