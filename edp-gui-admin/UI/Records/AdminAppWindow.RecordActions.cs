namespace edp_gui_admin;

public sealed partial class AdminAppWindow
{
    private async Task AddCurrentAsync()
    {
        try
        {
            switch (_recordTabs.SelectedTab?.Text)
            {
                case OwnersTab:
                    await AddOwnerAsync();
                    break;
                case SitesTab:
                    await AddSiteAsync();
                    break;
                case RisersTab:
                    await AddRiserAsync();
                    break;
                case RoomsTab:
                    await AddRoomAsync();
                    break;
                case TenantsTab:
                    await AddTenantAsync();
                    break;
                case DocumentsTab:
                    await AddDocumentAsync();
                    break;
            }

            await RefreshCurrentAsync();
        }
        catch (Exception ex)
        {
            _workspaceStatusLabel.Text = ex.Message;
        }
    }

    private async Task EditCurrentAsync()
    {
        try
        {
            switch (_recordTabs.SelectedTab?.Text)
            {
                case OwnersTab when _ownersSource.Current is AdminOwner owner:
                    await EditOwnerAsync(owner);
                    break;
                case SitesTab when _sitesSource.Current is AdminSite site:
                    await EditSiteAsync(site);
                    break;
                case RisersTab when _risersSource.Current is AdminRiser riser:
                    await EditRiserAsync(riser);
                    break;
                case RoomsTab when _roomsSource.Current is AdminRoom room:
                    await EditRoomAsync(room);
                    break;
                case TenantsTab when _tenantsSource.Current is AdminTenant tenant:
                    await EditTenantAsync(tenant);
                    break;
                case DocumentsTab when _documentsSource.Current is AdminDocument document:
                    await EditDocumentAsync(document);
                    break;
                case MaintenanceTab when _maintenanceSource.Current is AdminMaintenanceTicket ticket:
                    await EditMaintenanceTicketAsync(ticket);
                    break;
                default:
                    _workspaceStatusLabel.Text = "Select a record first.";
                    return;
            }

            await RefreshCurrentAsync();
        }
        catch (Exception ex)
        {
            _workspaceStatusLabel.Text = ex.Message;
        }
    }

    private async Task DeleteCurrentAsync()
    {
        if (MessageBox.Show("Delete the selected record?", "Confirm delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            switch (_recordTabs.SelectedTab?.Text)
            {
                case OwnersTab when _ownersSource.Current is AdminOwner owner:
                    await _recordService.DeleteOwnerAsync(owner.OwnerId);
                    break;
                case SitesTab when _sitesSource.Current is AdminSite site:
                    await _recordService.DeleteSiteAsync(site.SiteId);
                    break;
                case RisersTab when _risersSource.Current is AdminRiser riser:
                    await _recordService.DeleteRiserAsync(riser.RiserId);
                    break;
                case RoomsTab when _roomsSource.Current is AdminRoom room:
                    await _recordService.DeleteRoomAsync(room.RoomId);
                    break;
                case TenantsTab when _tenantsSource.Current is AdminTenant tenant:
                    await _recordService.DeleteTenantAsync(tenant.TenantId);
                    break;
                case DocumentsTab when _documentsSource.Current is AdminDocument document:
                    await _recordService.DeleteDocumentAsync(document.DocumentId);
                    break;
                case MaintenanceTab when _maintenanceSource.Current is AdminMaintenanceTicket ticket:
                    await DeleteMaintenanceTicketAsync(ticket);
                    break;
                default:
                    _workspaceStatusLabel.Text = "Select a record first.";
                    return;
            }

            await RefreshCurrentAsync();
        }
        catch (Exception ex)
        {
            _workspaceStatusLabel.Text = ex.Message;
        }
    }

    private async Task ResolveCurrentAsync()
    {
        try
        {
            switch (_recordTabs.SelectedTab?.Text)
            {
                case MaintenanceTab when _maintenanceSource.Current is AdminMaintenanceTicket ticket:
                    await ResolveMaintenanceTicketAsync(ticket);
                    break;
                default:
                    _workspaceStatusLabel.Text = "Select a maintenance ticket first.";
                    return;
            }

            await RefreshCurrentAsync();
        }
        catch (Exception ex)
        {
            _workspaceStatusLabel.Text = ex.Message;
        }
    }
}
