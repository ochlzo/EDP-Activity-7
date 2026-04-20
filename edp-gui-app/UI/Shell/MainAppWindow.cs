namespace edp_gui_app;

public sealed partial class MainAppWindow : Form
{
    private const int SidebarWidth = 400;

    private readonly SiteOwnerAuthService _authService;
    private readonly LoginFlowController _flowController = new();
    private readonly TableLayoutPanel _shell;
    private readonly Panel _sidebar;
    private readonly Panel _contentHost;
    private readonly Control _landingPanel;
    private readonly Control _loginPanel;
    private readonly Control _signUpPanel;
    private readonly Control _ownerSitesPanel;
    private readonly Control _siteDetailsPanel;
    private readonly Control _riserDetailsPanel;
    private readonly TextBox _emailTextBox;
    private readonly TextBox _passwordTextBox;
    private readonly Button _loginSubmitButton;
    private readonly TextBox _signUpNameTextBox;
    private readonly TextBox _signUpEmailTextBox;
    private readonly TextBox _signUpPasswordTextBox;
    private readonly Button _signUpSubmitButton;
    private readonly Label _loginStatusLabel;
    private readonly Label _signUpStatusLabel;
    private readonly TextBox _siteSearchTextBox;
    private readonly Button _refreshSitesButton;
    private readonly DataGridView _sitesGrid;
    private readonly Label _ownerSitesStatusLabel;
    private readonly Label _siteDetailsIdValueLabel;
    private readonly Label _siteDetailsNameValueLabel;
    private readonly DataGridView _siteRisersGrid;
    private readonly Label _siteRisersStatusLabel;
    private readonly Label _riserDetailsSiteNameValueLabel;
    private readonly Label _riserDetailsIdValueLabel;
    private readonly Label _riserDetailsNameValueLabel;
    private readonly ComboBox _riserRoomsSortComboBox;
    private readonly DataGridView _riserRoomsGrid;
    private readonly Label _riserRoomsStatusLabel;

    public MainAppWindow(SiteOwnerAuthService authService)
    {
        _authService = authService;

        Text = "Site Management System";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(920, 520);

        _shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        _shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SidebarWidth));
        _shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _sidebar = BuildSidebar();
        _contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28),
            BackColor = Color.WhiteSmoke
        };

        _landingPanel = BuildLandingPanel();
        (_loginPanel, _emailTextBox, _passwordTextBox, _loginSubmitButton, _loginStatusLabel) =
            BuildLoginPanel();
        (_signUpPanel, _signUpNameTextBox, _signUpEmailTextBox, _signUpPasswordTextBox, _signUpSubmitButton,
            _signUpStatusLabel) = BuildSignUpPanel();
        (_ownerSitesPanel, _siteSearchTextBox, _refreshSitesButton, _sitesGrid, _ownerSitesStatusLabel) =
            BuildOwnerSitesPanel();
        (_siteDetailsPanel, _siteDetailsIdValueLabel, _siteDetailsNameValueLabel, _siteRisersGrid,
            _siteRisersStatusLabel) = BuildSiteDetailsPanel();
        (_riserDetailsPanel, _riserDetailsSiteNameValueLabel, _riserDetailsIdValueLabel, _riserDetailsNameValueLabel,
            _riserRoomsSortComboBox, _riserRoomsGrid, _riserRoomsStatusLabel) = BuildRiserDetailsPanel();

        _contentHost.Controls.Add(_riserDetailsPanel);
        _contentHost.Controls.Add(_siteDetailsPanel);
        _contentHost.Controls.Add(_ownerSitesPanel);
        _contentHost.Controls.Add(_signUpPanel);
        _contentHost.Controls.Add(_loginPanel);
        _contentHost.Controls.Add(_landingPanel);

        _shell.Controls.Add(_sidebar, 0, 0);
        _shell.Controls.Add(_contentHost, 1, 0);
        Controls.Add(_shell);

        ResetOwnerWorkspace();
        ApplyState();
    }
}
