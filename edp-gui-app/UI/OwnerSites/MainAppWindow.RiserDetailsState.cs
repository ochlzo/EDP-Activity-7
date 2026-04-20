namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private readonly BindingSource _riserRoomsBindingSource = new();
    private IReadOnlyList<OwnedRoom> _loadedRooms = Array.Empty<OwnedRoom>();
    private string? _roomLoadError;

    private void ResetRiserDetailsState()
    {
        _loadedRooms = Array.Empty<OwnedRoom>();
        _roomLoadError = null;
        _riserRoomsBindingSource.DataSource = Array.Empty<OwnedRoom>();
        _riserRoomsGrid.Visible = false;
        _riserRoomsSortComboBox.SelectedIndex = 0;
        ShowRiserRoomsStatus(string.Empty, Color.DimGray);
        _riserDetailsSiteNameValueLabel.Text = "-";
        _riserDetailsIdValueLabel.Text = "-";
        _riserDetailsNameValueLabel.Text = "-";
    }

    private void UpdateRiserDetailsPanel()
    {
        var site = _flowController.SelectedSite;
        var riser = _flowController.SelectedRiser;
        _riserDetailsSiteNameValueLabel.Text = site?.SiteName ?? "-";
        _riserDetailsIdValueLabel.Text = riser?.RiserId.ToString() ?? "-";
        _riserDetailsNameValueLabel.Text = riser?.RiserName ?? "-";

        if (site is not null && riser is not null)
        {
            return;
        }

        _loadedRooms = Array.Empty<OwnedRoom>();
        _roomLoadError = null;
        _riserRoomsBindingSource.DataSource = Array.Empty<OwnedRoom>();
        _riserRoomsGrid.Visible = false;
        ShowRiserRoomsStatus(string.Empty, Color.DimGray);
    }

    private void ApplyRoomSort()
    {
        if (_roomLoadError is not null)
        {
            return;
        }

        var sortedRooms = OwnedRoomSort.Apply(_loadedRooms, GetSelectedRoomSortOrder()).ToArray();
        _riserRoomsBindingSource.DataSource = sortedRooms;
        _riserRoomsGrid.Visible = sortedRooms.Length > 0;

        var message = sortedRooms.Length == 0
            ? "No rooms are assigned to this riser yet."
            : string.Empty;
        ShowRiserRoomsStatus(message, Color.DimGray);
    }

    private RoomNameSortOrder GetSelectedRoomSortOrder()
    {
        return _riserRoomsSortComboBox.SelectedIndex == 1
            ? RoomNameSortOrder.Descending
            : RoomNameSortOrder.Ascending;
    }

    private void ShowRiserRoomsStatus(string text, Color color)
    {
        _riserRoomsStatusLabel.Text = text;
        _riserRoomsStatusLabel.ForeColor = color;
    }
}
