namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private readonly BindingSource _maintenanceBindingSource = new();
    private IReadOnlyList<OwnerMaintenanceRequest> _loadedMaintenance = Array.Empty<OwnerMaintenanceRequest>();

    private void ResetMaintenanceState()
    {
        _loadedMaintenance = Array.Empty<OwnerMaintenanceRequest>();
        _maintenanceBindingSource.DataSource = Array.Empty<OwnerMaintenanceRequest>();
        _maintenanceGrid.Visible = false;
        ShowMaintenanceStatus(string.Empty, Color.DimGray);
    }

    private void ShowMaintenanceStatus(string text, Color color)
    {
        _maintenanceStatusLabel.Text = text;
        _maintenanceStatusLabel.ForeColor = color;
    }
}
