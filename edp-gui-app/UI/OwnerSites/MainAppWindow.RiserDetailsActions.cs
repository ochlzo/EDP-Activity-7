namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private void OnRoomSortChanged(object? sender, EventArgs e)
    {
        ApplyRoomSort();
    }

    private async void OnRefreshRoomsClicked(object? sender, EventArgs e)
    {
        await ReloadRiserRoomsAsync();
    }

    private async void OnAddRoomClicked(object? sender, EventArgs e)
    {
        var owner = _flowController.CurrentOwner;
        var site = _flowController.SelectedSite;
        var riser = _flowController.SelectedRiser;
        if (owner is null || site is null || riser is null)
        {
            return;
        }

        using var dialog = BuildAddRoomDialog(owner.OwnerId, site.SiteId, riser.RiserId);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        await ReloadRiserRoomsAsync();
        await ReloadSiteRisersAsync();
    }

    private async Task ReloadRiserRoomsAsync()
    {
        var owner = _flowController.CurrentOwner;
        var site = _flowController.SelectedSite;
        var riser = _flowController.SelectedRiser;
        if (owner is null || site is null || riser is null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            _roomLoadError = null;
            _loadedRooms = Array.Empty<OwnedRoom>();
            _riserRoomsBindingSource.DataSource = Array.Empty<OwnedRoom>();
            _riserRoomsGrid.Visible = false;
            ShowRiserRoomsStatus("Loading rooms...", Color.DimGray);

            _loadedRooms = await _authService.LoadRoomsByRiserAsync(riser.RiserId, site.SiteId, owner.OwnerId);
            ApplyRoomSort();
        }
        catch (Exception ex)
        {
            _loadedRooms = Array.Empty<OwnedRoom>();
            _roomLoadError = $"Could not load rooms: {ex.Message}";
            _riserRoomsBindingSource.DataSource = Array.Empty<OwnedRoom>();
            _riserRoomsGrid.Visible = false;
            ShowRiserRoomsStatus(_roomLoadError, Color.Firebrick);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnRiserRoomsGridCellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        if (_riserRoomsGrid.Rows[e.RowIndex].DataBoundItem is not OwnedRoom room)
        {
            return;
        }

        var owner = _flowController.CurrentOwner;
        var site = _flowController.SelectedSite;
        var riser = _flowController.SelectedRiser;
        if (owner is null || site is null || riser is null)
        {
            return;
        }

        var columnName = _riserRoomsGrid.Columns[e.ColumnIndex].Name;
        if (columnName == "UpdateRoom")
        {
            using var dialog = BuildEditRoomDialog(owner.OwnerId, site.SiteId, room);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            await ReloadRiserRoomsAsync();
            await ReloadSiteRisersAsync();
            return;
        }

        if (columnName != "DeleteRoom")
        {
            return;
        }

        if (MessageBox.Show(
                this,
                $"Delete room '{room.RoomName}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            await _authService.DeleteRoomAsync(room.RoomId, site.SiteId, owner.OwnerId);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Delete Room", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        await ReloadRiserRoomsAsync();
        await ReloadSiteRisersAsync();
    }
}
