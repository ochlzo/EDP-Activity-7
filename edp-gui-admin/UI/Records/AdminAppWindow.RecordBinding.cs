namespace edp_gui_admin;

public sealed partial class AdminAppWindow
{
    private async Task RefreshAllAsync()
    {
        await RefreshOwnersAsync();
        await RefreshSitesAsync();
        await RefreshRisersAsync();
        await RefreshRoomsAsync();
        await RefreshTenantsAsync();
        await RefreshDocumentsAsync();
        await RefreshOccupancyAsync();
        await RefreshMaintenanceAsync();
        await RefreshActivityAsync();
        await RefreshReportsAsync();
    }

    private async Task RefreshCurrentAsync()
    {
        _workspaceStatusLabel.Text = string.Empty;

        try
        {
            switch (_recordTabs.SelectedTab?.Text)
            {
                case OwnersTab:
                    await RefreshOwnersAsync();
                    break;
                case SitesTab:
                    await RefreshSitesAsync();
                    break;
                case RisersTab:
                    await RefreshRisersAsync();
                    break;
                case RoomsTab:
                    await RefreshRoomsAsync();
                    break;
                case TenantsTab:
                    await RefreshTenantsAsync();
                    break;
                case DocumentsTab:
                    await RefreshDocumentsAsync();
                    break;
                case OccupancyTab:
                    await RefreshOccupancyAsync();
                    break;
                case MaintenanceTab:
                    await RefreshMaintenanceAsync();
                    break;
                case ActivityLogTab:
                    await RefreshActivityAsync();
                    break;
                case ReportsTab:
                    await RefreshReportsAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            _workspaceStatusLabel.Text = ex.Message;
        }
    }

    private async Task RefreshOwnersAsync()
    {
        _allOwners = await _recordService.LoadOwnersAsync();
        ApplyOwnerSearchFilter();
    }

    private async Task RefreshSitesAsync()
    {
        _allSites = await _recordService.LoadSitesAsync();
        ApplySiteSearchFilter();
    }

    private async Task RefreshRisersAsync()
    {
        _allRisers = await _recordService.LoadRisersAsync();
        ApplyRiserSearchFilter();
    }

    private async Task RefreshRoomsAsync()
    {
        _allRooms = await _recordService.LoadRoomsAsync();
        ApplyRoomSearchFilter();
    }

    private async Task RefreshTenantsAsync()
    {
        _allTenants = await _recordService.LoadTenantsAsync();
        ApplyTenantSearchFilter();
    }

    private async Task RefreshDocumentsAsync()
    {
        _allDocuments = await _recordService.LoadDocumentsAsync();
        ApplyDocumentSearchFilter();
    }

    private async Task RefreshOccupancyAsync()
    {
        _allOccupancy = await _transactionService.LoadOccupancyHistoryAsync(_occupancyRoomFilterId);
        ApplyOccupancySearchFilter();
    }

    private async Task RefreshMaintenanceAsync()
    {
        _allMaintenance = await _transactionService.LoadMaintenanceTicketsAsync();
        ApplyMaintenanceSearchFilter();
    }

    private async Task RefreshActivityAsync()
    {
        _allActivity = await _transactionService.LoadActivityLogsAsync();
        ApplyActivitySearchFilter();
    }

    private async Task RefreshReportsAsync()
    {
        _allReportRows = await _transactionService.LoadReportRowsAsync();
        ApplyReportSelection();
    }
}
