namespace edp_gui_admin;

public sealed partial class AdminAppWindow
{
    private void ApplyOccupancySearchFilter()
    {
        var searchText = _occupancySearchTextBox.Text.Trim();
        var records = _occupancyRoomFilterId is null
            ? _allOccupancy
            : _allOccupancy.Where(history => history.RoomId == _occupancyRoomFilterId.Value).ToArray();
        SetParentFilterUi(_occupancyParentFilterLabel, _occupancyClearParentFilterButton, _occupancyParentFilterText);
        _occupancySource.DataSource = FilterRecords(
            records,
            searchText,
            history => Matches(history.HistoryId.ToString(), searchText) ||
                Matches(history.TenantName, searchText) ||
                Matches(history.TenantId?.ToString() ?? string.Empty, searchText) ||
                Matches(history.RoomName, searchText) ||
                Matches(history.RoomId.ToString(), searchText) ||
                Matches(history.DateOccupied.ToString("yyyy-MM-dd HH:mm"), searchText) ||
                Matches(history.Notes, searchText));
    }

    private void ApplyMaintenanceSearchFilter()
    {
        var searchText = _maintenanceSearchTextBox.Text.Trim();
        _maintenanceSource.DataSource = FilterRecords(
            _allMaintenance,
            searchText,
            ticket => Matches(ticket.TicketId.ToString(), searchText) ||
                Matches(ticket.Title, searchText) ||
                Matches(ticket.SiteName, searchText) ||
                Matches(ticket.Description, searchText) ||
                Matches(ticket.Priority, searchText) ||
                Matches(ticket.Status, searchText) ||
                Matches(ticket.Notes, searchText) ||
                Matches(ticket.RequestedAt.ToString("yyyy-MM-dd HH:mm"), searchText) ||
                Matches(ticket.ResolvedAt?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty, searchText));
    }

    private void ApplyActivitySearchFilter()
    {
        var searchText = _activitySearchTextBox.Text.Trim();
        _activitySource.DataSource = FilterRecords(
            _allActivity,
            searchText,
            log => Matches(log.ActorName, searchText) ||
                Matches(log.Action, searchText) ||
                Matches(log.EntityType, searchText) ||
                Matches(log.Description, searchText));
    }
}
