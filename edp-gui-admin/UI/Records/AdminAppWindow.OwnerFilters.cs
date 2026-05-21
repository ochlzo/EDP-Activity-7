namespace edp_gui_admin;

public sealed partial class AdminAppWindow
{
    private void ApplyOwnerSearchFilter()
    {
        var searchText = _ownerSearchTextBox.Text.Trim();
        var owners = string.IsNullOrWhiteSpace(searchText)
            ? _allOwners
            : _allOwners
                .Where(owner =>
                    owner.OwnerName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    owner.OwnerEmail.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToArray();

        try
        {
            _isUpdatingOwnerStatus = true;
            _ownersSource.DataSource = owners;
        }
        finally
        {
            _isUpdatingOwnerStatus = false;
        }
    }

    private void ApplySiteSearchFilter()
    {
        var searchText = _siteSearchTextBox.Text.Trim();
        var records = _siteOwnerFilterId is null
            ? _allSites
            : _allSites.Where(site => site.OwnerId == _siteOwnerFilterId.Value).ToArray();
        SetParentFilterUi(_sitesParentFilterLabel, _sitesClearParentFilterButton, _siteParentFilterText);
        _sitesSource.DataSource = FilterRecords(
            records,
            searchText,
            site => Matches(site.SiteName, searchText) ||
                Matches(site.OwnerName, searchText));
    }

    private void ApplyRiserSearchFilter()
    {
        var searchText = _riserSearchTextBox.Text.Trim();
        var records = _riserSiteFilterId is null
            ? _allRisers
            : _allRisers.Where(riser => riser.SiteId == _riserSiteFilterId.Value).ToArray();
        SetParentFilterUi(_risersParentFilterLabel, _risersClearParentFilterButton, _riserParentFilterText);
        _risersSource.DataSource = FilterRecords(
            records,
            searchText,
            riser => Matches(riser.RiserName, searchText) ||
                Matches(riser.SiteName, searchText));
    }

    private void ApplyRoomSearchFilter()
    {
        var searchText = _roomSearchTextBox.Text.Trim();
        var records = _roomRiserFilterId is null
            ? _allRooms
            : _allRooms.Where(room => room.RiserId == _roomRiserFilterId.Value).ToArray();
        SetParentFilterUi(_roomsParentFilterLabel, _roomsClearParentFilterButton, _roomParentFilterText);
        _roomsSource.DataSource = FilterRecords(
            records,
            searchText,
            room => Matches(room.RoomName, searchText) ||
                Matches(room.RiserName, searchText) ||
                Matches(room.TenantName, searchText) ||
                Matches(room.Occupancy, searchText));
    }

    private void ApplyTenantSearchFilter()
    {
        var searchText = _tenantSearchTextBox.Text.Trim();
        var records = _tenantRoomFilterId is null
            ? _allTenants
            : _tenantFilterId is null
                ? []
                : _allTenants.Where(tenant => tenant.TenantId == _tenantFilterId.Value).ToArray();
        SetParentFilterUi(_tenantsParentFilterLabel, _tenantsClearParentFilterButton, _tenantParentFilterText);
        _tenantsSource.DataSource = FilterRecords(
            records,
            searchText,
            tenant => Matches(tenant.TenantName, searchText) ||
                Matches(tenant.TenantEmail, searchText) ||
                Matches(tenant.TenantAddress, searchText) ||
                Matches(tenant.TenantContactNumber, searchText));
    }

    private void ApplyDocumentSearchFilter()
    {
        var searchText = _documentSearchTextBox.Text.Trim();
        var records = _documentTenantFilterId is null
            ? _allDocuments
            : _allDocuments.Where(document => document.TenantId == _documentTenantFilterId.Value).ToArray();
        SetParentFilterUi(_documentsParentFilterLabel, _documentsClearParentFilterButton, _documentParentFilterText);
        _documentsSource.DataSource = FilterRecords(
            records,
            searchText,
            document => Matches(document.DocumentName, searchText) ||
                Matches(document.TenantName, searchText));
    }

    private async Task OnOwnerStatusChangedAsync(DataGridView grid, DataGridViewCellEventArgs e)
    {
        if (_isUpdatingOwnerStatus || e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        if (grid.Columns[e.ColumnIndex].DataPropertyName != nameof(AdminOwner.Status) ||
            grid.Rows[e.RowIndex].DataBoundItem is not AdminOwner owner)
        {
            return;
        }

        try
        {
            _workspaceStatusLabel.Text = "Updating owner status...";
            await _recordService.UpdateOwnerStatusAsync(owner.OwnerId, owner.IsActive);
            _workspaceStatusLabel.Text = string.Empty;
        }
        catch (Exception ex)
        {
            _workspaceStatusLabel.Text = ex.Message;
            await RefreshOwnersAsync();
        }
    }

    private static IReadOnlyList<T> FilterRecords<T>(
        IReadOnlyList<T> records,
        string searchText,
        Func<T, bool> predicate)
    {
        return string.IsNullOrWhiteSpace(searchText)
            ? records
            : records.Where(predicate).ToArray();
    }

    private static bool Matches(string value, string searchText) =>
        value.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    private static void SetParentFilterUi(Label label, Button clearButton, string? text)
    {
        var hasFilter = !string.IsNullOrWhiteSpace(text);
        label.Text = text ?? string.Empty;
        label.Visible = hasFilter;
        clearButton.Visible = hasFilter;
    }
}
