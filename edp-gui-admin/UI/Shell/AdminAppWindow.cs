namespace edp_gui_admin;

public sealed partial class AdminAppWindow : Form
{
    private readonly AdminAuthService _authService;
    private readonly AdminRecordService _recordService;
    private readonly AdminTransactionService _transactionService;
    private readonly Panel _contentHost;
    private readonly Control _loginPanel;
    private readonly Control _workspacePanel;
    private readonly TextBox _emailTextBox;
    private readonly TextBox _passwordTextBox;
    private readonly Label _loginStatusLabel;
    private readonly Label _workspaceStatusLabel;
    private readonly TabControl _recordTabs;
    private readonly TextBox _ownerSearchTextBox;
    private readonly TextBox _siteSearchTextBox;
    private readonly TextBox _riserSearchTextBox;
    private readonly TextBox _roomSearchTextBox;
    private readonly TextBox _tenantSearchTextBox;
    private readonly TextBox _documentSearchTextBox;
    private readonly TextBox _occupancySearchTextBox;
    private readonly TextBox _maintenanceSearchTextBox;
    private readonly TextBox _activitySearchTextBox;
    private readonly BindingSource _ownersSource = new();
    private readonly BindingSource _sitesSource = new();
    private readonly BindingSource _risersSource = new();
    private readonly BindingSource _roomsSource = new();
    private readonly BindingSource _tenantsSource = new();
    private readonly BindingSource _documentsSource = new();
    private readonly BindingSource _occupancySource = new();
    private readonly BindingSource _maintenanceSource = new();
    private readonly BindingSource _activitySource = new();
    private readonly BindingSource _reportSource = new();
    private ComboBox _reportSelector = null!;
    private TextBox _reportSignatoryTextBox = null!;
    private Label _reportStatusLabel = null!;
    private Label _sitesParentFilterLabel = null!;
    private Label _risersParentFilterLabel = null!;
    private Label _roomsParentFilterLabel = null!;
    private Label _tenantsParentFilterLabel = null!;
    private Label _documentsParentFilterLabel = null!;
    private Label _occupancyParentFilterLabel = null!;
    private Button _sitesClearParentFilterButton = null!;
    private Button _risersClearParentFilterButton = null!;
    private Button _roomsClearParentFilterButton = null!;
    private Button _tenantsClearParentFilterButton = null!;
    private Button _documentsClearParentFilterButton = null!;
    private Button _occupancyClearParentFilterButton = null!;
    private IReadOnlyList<AdminOwner> _allOwners = [];
    private IReadOnlyList<AdminSite> _allSites = [];
    private IReadOnlyList<AdminRiser> _allRisers = [];
    private IReadOnlyList<AdminRoom> _allRooms = [];
    private IReadOnlyList<AdminTenant> _allTenants = [];
    private IReadOnlyList<AdminDocument> _allDocuments = [];
    private IReadOnlyList<AdminOccupancyHistoryRow> _allOccupancy = [];
    private IReadOnlyList<AdminMaintenanceTicket> _allMaintenance = [];
    private IReadOnlyList<AdminActivityLog> _allActivity = [];
    private IReadOnlyList<AdminReportRow> _allReportRows = [];
    private int? _siteOwnerFilterId;
    private int? _riserSiteFilterId;
    private int? _roomRiserFilterId;
    private int? _tenantRoomFilterId;
    private int? _tenantFilterId;
    private int? _documentTenantFilterId;
    private int? _occupancyRoomFilterId;
    private string? _siteParentFilterText;
    private string? _riserParentFilterText;
    private string? _roomParentFilterText;
    private string? _tenantParentFilterText;
    private string? _documentParentFilterText;
    private string? _occupancyParentFilterText;
    private bool _isUpdatingOwnerStatus;

    public AdminAppWindow(
        AdminAuthService authService,
        AdminRecordService recordService,
        AdminTransactionService transactionService)
    {
        _authService = authService;
        _recordService = recordService;
        _transactionService = transactionService;

        Text = "Admin User Management";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(1440, 860);

        _contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.WhiteSmoke,
            Padding = new Padding(24)
        };

        (_loginPanel, _emailTextBox, _passwordTextBox, _loginStatusLabel) = BuildLoginPanel();
        (_workspacePanel, _recordTabs, _workspaceStatusLabel, _ownerSearchTextBox, _siteSearchTextBox,
            _riserSearchTextBox, _roomSearchTextBox, _tenantSearchTextBox, _documentSearchTextBox,
            _occupancySearchTextBox, _maintenanceSearchTextBox, _activitySearchTextBox) =
            BuildWorkspacePanel();

        _contentHost.Controls.Add(_workspacePanel);
        _contentHost.Controls.Add(_loginPanel);
        Controls.Add(_contentHost);

        ShowLogin();
    }

    private void ShowLogin()
    {
        _workspacePanel.Visible = false;
        _loginPanel.Visible = true;
        _loginPanel.BringToFront();
        _passwordTextBox.Clear();
    }

    private async Task ShowWorkspaceAsync()
    {
        _loginPanel.Visible = false;
        _workspacePanel.Visible = true;
        _workspacePanel.BringToFront();
        await RefreshAllAsync();
    }
}
