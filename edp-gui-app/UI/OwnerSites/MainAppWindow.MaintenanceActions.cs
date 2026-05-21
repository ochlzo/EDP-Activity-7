namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private async Task ShowMaintenanceViewAsync()
    {
        _flowController.ShowMaintenance();
        ApplyState();
        await ReloadOwnerMaintenanceAsync();
    }

    private async void OnRefreshMaintenanceClicked(object? sender, EventArgs e)
    {
        await ReloadOwnerMaintenanceAsync();
    }

    private async void OnAddMaintenanceClicked(object? sender, EventArgs e)
    {
        var owner = _flowController.CurrentOwner;
        if (owner is null || _maintenanceService is null)
        {
            ShowMaintenanceStatus("Maintenance service is not configured.", Color.Firebrick);
            return;
        }

        if (_loadedSites.Count == 0)
        {
            await ReloadOwnedSitesAsync();
        }

        using var dialog = BuildMaintenanceDialog(owner.OwnerId, _loadedSites);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        await ReloadOwnerMaintenanceAsync();
    }

    private async Task ReloadOwnerMaintenanceAsync()
    {
        var owner = _flowController.CurrentOwner;
        if (owner is null || _maintenanceService is null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            ShowMaintenanceStatus("Loading maintenance requests...", Color.DimGray);
            _loadedMaintenance = await _maintenanceService.LoadTicketsByOwnerAsync(owner.OwnerId);
            _maintenanceBindingSource.DataSource = _loadedMaintenance;
            _maintenanceGrid.Visible = _loadedMaintenance.Count > 0;
            ShowMaintenanceStatus(
                _loadedMaintenance.Count == 0 ? "No maintenance requests yet." : string.Empty,
                Color.DimGray);
        }
        catch (Exception ex)
        {
            _loadedMaintenance = Array.Empty<OwnerMaintenanceRequest>();
            _maintenanceBindingSource.DataSource = Array.Empty<OwnerMaintenanceRequest>();
            _maintenanceGrid.Visible = false;
            ShowMaintenanceStatus($"Could not load maintenance requests: {ex.Message}", Color.Firebrick);
        }
        finally
        {
            SetBusy(false);
        }
    }
}
