namespace edp_gui_admin;

public sealed partial class AdminAppWindow
{
    private const string OwnersTab = "Site Owners";
    private const string SitesTab = "Sites";
    private const string RisersTab = "Risers";
    private const string RoomsTab = "Rooms";
    private const string TenantsTab = "Tenants";
    private const string DocumentsTab = "Documents";
    private const string OccupancyTab = "Occupancy History";
    private const string MaintenanceTab = "Maintenance";
    private const string ActivityLogTab = "Activity Log";
    private const string ReportsTab = "Reports";

    private (
        Control Panel,
        TabControl Tabs,
        Label Status,
        TextBox OwnerSearch,
        TextBox SiteSearch,
        TextBox RiserSearch,
        TextBox RoomSearch,
        TextBox TenantSearch,
        TextBox DocumentSearch,
        TextBox OccupancySearch,
        TextBox MaintenanceSearch,
        TextBox ActivitySearch) BuildWorkspacePanel()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        var ownerSearch = new TextBox { Width = 280, PlaceholderText = "Search name or email" };
        var siteSearch = new TextBox { Width = 280, PlaceholderText = "Search site or owner" };
        var riserSearch = new TextBox { Width = 280, PlaceholderText = "Search riser or site" };
        var roomSearch = new TextBox { Width = 280, PlaceholderText = "Search room, riser, or tenant" };
        var tenantSearch = new TextBox { Width = 280, PlaceholderText = "Search tenant, email, address, or contact" };
        var documentSearch = new TextBox { Width = 280, PlaceholderText = "Search document or tenant" };
        var occupancySearch = new TextBox { Width = 280, PlaceholderText = "Search history, room, tenant, or notes" };
        var maintenanceSearch = new TextBox { Width = 280, PlaceholderText = "Search ticket, site, title, description, status, or notes" };
        var activitySearch = new TextBox { Width = 280, PlaceholderText = "Search activity" };
        ownerSearch.TextChanged += (_, _) => ApplyOwnerSearchFilter();
        siteSearch.TextChanged += (_, _) => ApplySiteSearchFilter();
        riserSearch.TextChanged += (_, _) => ApplyRiserSearchFilter();
        roomSearch.TextChanged += (_, _) => ApplyRoomSearchFilter();
        tenantSearch.TextChanged += (_, _) => ApplyTenantSearchFilter();
        documentSearch.TextChanged += (_, _) => ApplyDocumentSearchFilter();
        occupancySearch.TextChanged += (_, _) => ApplyOccupancySearchFilter();
        maintenanceSearch.TextChanged += (_, _) => ApplyMaintenanceSearchFilter();
        activitySearch.TextChanged += (_, _) => ApplyActivitySearchFilter();

        tabs.TabPages.Add(BuildOwnersTab(ownerSearch));
        tabs.TabPages.Add(BuildTab(SitesTab, _sitesSource, true, siteSearch));
        tabs.TabPages.Add(BuildTab(RisersTab, _risersSource, true, riserSearch));
        tabs.TabPages.Add(BuildTab(RoomsTab, _roomsSource, true, roomSearch));
        tabs.TabPages.Add(BuildTab(TenantsTab, _tenantsSource, true, tenantSearch));
        tabs.TabPages.Add(BuildTab(DocumentsTab, _documentsSource, true, documentSearch));
        tabs.TabPages.Add(BuildOccupancyHistoryTab(occupancySearch));
        tabs.TabPages.Add(BuildMaintenanceTab(maintenanceSearch));
        tabs.TabPages.Add(BuildTab(ActivityLogTab, _activitySource, false, activitySearch));
        tabs.TabPages.Add(BuildReportsTab());
        tabs.SelectedIndexChanged += async (_, _) => await RefreshCurrentAsync();

        var refresh = BuildActionButton("Refresh", async (_, _) => await RefreshCurrentAsync());
        var logout = BuildActionButton("Logout", (_, _) => ShowLogin());
        var status = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(760, 0),
            ForeColor = Color.Firebrick
        };

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        actions.Controls.Add(refresh);
        actions.Controls.Add(logout);

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            Margin = new Padding(0, 0, 0, 14)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Admin Records",
            Font = new Font(Font.FontFamily, 20, FontStyle.Bold)
        }, 0, 0);
        header.Controls.Add(actions, 1, 0);

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.White,
            Padding = new Padding(18)
        };
        panel.RowStyles.Add(new RowStyle());
        panel.RowStyles.Add(new RowStyle());
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(header, 0, 0);
        panel.Controls.Add(status, 0, 1);
        panel.Controls.Add(tabs, 0, 2);

        return (panel, tabs, status, ownerSearch, siteSearch, riserSearch, roomSearch, tenantSearch, documentSearch,
            occupancySearch, maintenanceSearch, activitySearch);
    }

    private TabPage BuildOwnersTab(TextBox search)
    {
        var grid = BuildRecordGrid(_ownersSource);
        grid.CellDoubleClick += async (_, e) => await OpenChildRecordsAsync(e.RowIndex);
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(AdminOwner.OwnerId),
            HeaderText = "Owner ID",
            ReadOnly = true
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(AdminOwner.OwnerName),
            HeaderText = "Name",
            ReadOnly = true
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(AdminOwner.OwnerEmail),
            HeaderText = "Email",
            ReadOnly = true
        });
        grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            DataPropertyName = nameof(AdminOwner.Status),
            HeaderText = "Status",
            DataSource = new[] { "Active", "Inactive" }
        });
        grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (grid.IsCurrentCellDirty)
            {
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
        grid.CellValueChanged += async (_, e) => await OnOwnerStatusChangedAsync(grid, e);
        grid.DataError += (_, e) => e.ThrowException = false;

        var actions = BuildEditableActions();
        actions.Controls.Add(BuildSearchPanel(search));

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        panel.RowStyles.Add(new RowStyle());
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(actions, 0, 0);
        panel.Controls.Add(grid, 0, 1);

        return new TabPage(OwnersTab) { Controls = { panel } };
    }

    private TabPage BuildTab(string title, BindingSource source, bool editable, TextBox? search = null)
    {
        var grid = BuildRecordGrid(source);
        grid.ReadOnly = true;
        grid.AutoGenerateColumns = true;
        grid.CellDoubleClick += async (_, e) => await OpenChildRecordsAsync(e.RowIndex);

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        panel.RowStyles.Add(new RowStyle());
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        if (editable)
        {
            var actions = BuildEditableActions();
            if (search is not null)
            {
                actions.Controls.Add(BuildSearchPanel(search));
            }

            AddParentFilterControls(title, actions);

            panel.Controls.Add(actions, 0, 0);
        }
        else if (search is not null)
        {
            panel.Controls.Add(BuildSearchPanel(search), 0, 0);
        }
        else
        {
            panel.Controls.Add(new Label { AutoSize = true, Text = "Read-only records" }, 0, 0);
        }

        panel.Controls.Add(grid, 0, 1);
        return new TabPage(title) { Controls = { panel } };
    }

    private void AddParentFilterControls(string title, FlowLayoutPanel actions)
    {
        var label = BuildParentFilterLabel();
        var clear = BuildActionButton("Clear filter", (_, _) => ClearParentFilter(title));
        clear.Visible = false;
        actions.Controls.Add(label);
        actions.Controls.Add(clear);

        switch (title)
        {
            case SitesTab:
                _sitesParentFilterLabel = label;
                _sitesClearParentFilterButton = clear;
                break;
            case RisersTab:
                _risersParentFilterLabel = label;
                _risersClearParentFilterButton = clear;
                break;
            case RoomsTab:
                _roomsParentFilterLabel = label;
                _roomsClearParentFilterButton = clear;
                break;
            case TenantsTab:
                _tenantsParentFilterLabel = label;
                _tenantsClearParentFilterButton = clear;
                break;
            case DocumentsTab:
                _documentsParentFilterLabel = label;
                _documentsClearParentFilterButton = clear;
                break;
            case OccupancyTab:
                _occupancyParentFilterLabel = label;
                _occupancyClearParentFilterButton = clear;
                break;
        }
    }

    private static Label BuildParentFilterLabel() => new()
    {
        AutoSize = true,
        ForeColor = Color.DimGray,
        Margin = new Padding(10, 6, 0, 8),
        Visible = false
    };

    private static FlowLayoutPanel BuildSearchPanel(TextBox search)
    {
        var searchPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(10, 0, 0, 8)
        };
        searchPanel.Controls.Add(new Label { AutoSize = true, Text = "Search", Margin = new Padding(0, 6, 8, 0) });
        searchPanel.Controls.Add(search);
        return searchPanel;
    }

    private DataGridView BuildRecordGrid(BindingSource source) => new()
    {
        Dock = DockStyle.Fill,
        AutoGenerateColumns = false,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        MultiSelect = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle,
        DataSource = source
    };

    private FlowLayoutPanel BuildEditableActions()
    {
        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 8)
        };
        actions.Controls.Add(BuildActionButton("Add", async (_, _) => await AddCurrentAsync()));
        actions.Controls.Add(BuildActionButton("Edit", async (_, _) => await EditCurrentAsync()));
        actions.Controls.Add(BuildActionButton("Delete", async (_, _) => await DeleteCurrentAsync()));
        return actions;
    }
}
