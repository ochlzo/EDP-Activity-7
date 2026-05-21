namespace edp_gui_admin;

public sealed partial class AdminAppWindow
{
    private async Task OpenChildRecordsAsync(int rowIndex)
    {
        if (rowIndex < 0)
        {
            return;
        }

        switch (_recordTabs.SelectedTab?.Text)
        {
            case OwnersTab when _ownersSource.Current is AdminOwner owner:
                _siteOwnerFilterId = owner.OwnerId;
                _siteParentFilterText = $"Showing sites for owner: {owner.OwnerName}";
                await SelectTabAndRefreshAsync(SitesTab);
                break;
            case SitesTab when _sitesSource.Current is AdminSite site:
                _riserSiteFilterId = site.SiteId;
                _riserParentFilterText = $"Showing risers for site: {site.SiteName}";
                await SelectTabAndRefreshAsync(RisersTab);
                break;
            case RisersTab when _risersSource.Current is AdminRiser riser:
                _roomRiserFilterId = riser.RiserId;
                _roomParentFilterText = $"Showing rooms for riser: {riser.RiserName}";
                await SelectTabAndRefreshAsync(RoomsTab);
                break;
            case RoomsTab when _roomsSource.Current is AdminRoom room:
                _occupancyRoomFilterId = room.RoomId;
                _occupancyParentFilterText = $"Showing occupancy history for room: {room.RoomName}";
                await SelectTabAndRefreshAsync(OccupancyTab);
                break;
            case TenantsTab when _tenantsSource.Current is AdminTenant tenant:
                _documentTenantFilterId = tenant.TenantId;
                _documentParentFilterText = $"Showing documents for tenant: {tenant.TenantName}";
                await SelectTabAndRefreshAsync(DocumentsTab);
                break;
        }
    }

    private async Task SelectTabAndRefreshAsync(string tabName)
    {
        foreach (TabPage tabPage in _recordTabs.TabPages)
        {
            if (tabPage.Text == tabName)
            {
                _recordTabs.SelectedTab = tabPage;
                break;
            }
        }

        await RefreshCurrentAsync();
    }

    private void ClearParentFilter(string tabName)
    {
        switch (tabName)
        {
            case SitesTab:
                _siteOwnerFilterId = null;
                _siteParentFilterText = null;
                ApplySiteSearchFilter();
                break;
            case RisersTab:
                _riserSiteFilterId = null;
                _riserParentFilterText = null;
                ApplyRiserSearchFilter();
                break;
            case RoomsTab:
                _roomRiserFilterId = null;
                _roomParentFilterText = null;
                ApplyRoomSearchFilter();
                break;
            case TenantsTab:
                _tenantRoomFilterId = null;
                _tenantFilterId = null;
                _tenantParentFilterText = null;
                ApplyTenantSearchFilter();
                break;
            case DocumentsTab:
                _documentTenantFilterId = null;
                _documentParentFilterText = null;
                ApplyDocumentSearchFilter();
                break;
            case OccupancyTab:
                _occupancyRoomFilterId = null;
                _occupancyParentFilterText = null;
                ApplyOccupancySearchFilter();
                break;
        }
    }
}
