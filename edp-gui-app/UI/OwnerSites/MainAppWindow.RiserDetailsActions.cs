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

        if (columnName == "AddTenant")
        {
            if (room.IsOccupied)
            {
                MessageBox.Show(
                    this,
                    "This room already has a tenant.",
                    "Add Tenant",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var dialog = BuildAddTenantToRoomDialog(owner.OwnerId, site.SiteId, room);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            await ReloadRiserRoomsAsync();
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

    private async void OnRiserRoomsGridCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
            _riserRoomsGrid.Columns[e.ColumnIndex].Name != "Tenant" ||
            _riserRoomsGrid.Rows[e.RowIndex].DataBoundItem is not OwnedRoom { IsOccupied: true } room ||
            room.TenantId is null)
        {
            return;
        }

        await ShowTenantDetailsAsync(room);
    }

    private void OnRiserRoomsGridCellMouseMove(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
            _riserRoomsGrid.Columns[e.ColumnIndex].Name != "Tenant" ||
            _riserRoomsGrid.Rows[e.RowIndex].DataBoundItem is not OwnedRoom { IsOccupied: true })
        {
            _riserRoomsGrid.Cursor = Cursors.Default;
            return;
        }

        _riserRoomsGrid.Cursor = Cursors.Hand;
        _riserRoomsGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.Font = new Font(Font, FontStyle.Underline);
    }

    private void OnRiserRoomsGridCellMouseLeave(object? sender, DataGridViewCellEventArgs e)
    {
        _riserRoomsGrid.Cursor = Cursors.Default;
        foreach (DataGridViewRow row in _riserRoomsGrid.Rows)
        {
            if (_riserRoomsGrid.Columns.Contains("Tenant"))
            {
                row.Cells["Tenant"].Style.Font = null;
            }
        }
    }

    private async Task ShowTenantDetailsAsync(OwnedRoom room)
    {
        var owner = _flowController.CurrentOwner;
        var site = _flowController.SelectedSite;
        if (owner is null || site is null || room.TenantId is null)
        {
            return;
        }

        try
        {
            while (true)
            {
                SetBusy(true);
                var tenant = await _authService.LoadTenantDetailsAsync(room.TenantId.Value, site.SiteId, owner.OwnerId);
                var documents = await _authService.LoadTenantDocumentsAsync(room.TenantId.Value, site.SiteId, owner.OwnerId);
                using var dialog = BuildTenantDetailsDialog(
                    tenant,
                    documents,
                    () => SendDocumentRequestAsync(tenant));
                SetBusy(false);

                var result = dialog.ShowDialog(this);
                if (result == DialogResult.Retry)
                {
                    continue;
                }

                if (result != DialogResult.OK)
                {
                    if (result == DialogResult.Yes)
                    {
                        using var replaceDialog = BuildReplaceTenantDialog(owner.OwnerId, site.SiteId, room);
                        if (replaceDialog.ShowDialog(this) != DialogResult.OK)
                        {
                            continue;
                        }

                        await ReloadRiserRoomsAsync();
                        await ReloadSiteRisersAsync();
                        break;
                    }

                    break;
                }

                using var editDialog = BuildEditTenantDialog(owner.OwnerId, site.SiteId, tenant);
                if (editDialog.ShowDialog(this) != DialogResult.OK)
                {
                    continue;
                }

                await ReloadRiserRoomsAsync();
            }
        }
        catch (Exception ex)
        {
            SetBusy(false);
            MessageBox.Show(this, ex.Message, "Tenant Details", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task<bool> SendDocumentRequestAsync(OwnedTenant tenant)
    {
        var requestedDocuments = ShowDocumentRequestPicker();
        if (requestedDocuments is null || requestedDocuments.Count == 0)
        {
            return false;
        }

        _documentRequestServer.EnsureStarted();
        var requestUrl = _documentRequestServer.RegisterRequest(
            tenant.TenantId,
            tenant.Name,
            requestedDocuments);
        var owner = _flowController.CurrentOwner;
        var site = _flowController.SelectedSite;
        if (owner is null || site is null)
        {
            return false;
        }

        await _authService.CreatePendingTenantDocumentsAsync(
            tenant.TenantId,
            site.SiteId,
            owner.OwnerId,
            requestedDocuments);
        await _authService.SendDocumentRequestEmailAsync(tenant, requestUrl, requestedDocuments);
        return true;
    }
}
