namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private void ShowSiteDetailsView()
    {
        _flowController.ShowSiteDetails();
        ApplyState();
        ApplyRiserList();
    }

    private async void OnRefreshRisersClicked(object? sender, EventArgs e)
    {
        await ReloadSiteRisersAsync();
    }

    private async void OnAddRiserClicked(object? sender, EventArgs e)
    {
        var owner = _flowController.CurrentOwner;
        var site = _flowController.SelectedSite;
        if (owner is null || site is null)
        {
            return;
        }

        using var dialog = BuildAddRiserDialog(owner.OwnerId, site.SiteId);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        await ReloadSiteRisersAsync();
    }

    private async Task ReloadSiteRisersAsync()
    {
        var owner = _flowController.CurrentOwner;
        var site = _flowController.SelectedSite;
        if (owner is null || site is null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            _riserLoadError = null;
            _loadedRisers = Array.Empty<OwnedRiser>();
            _siteRisersBindingSource.DataSource = Array.Empty<OwnedRiser>();
            _siteRisersGrid.Visible = false;
            ShowSiteRisersStatus("Loading risers...", Color.DimGray);

            _loadedRisers = await _authService.LoadRisersBySiteAsync(site.SiteId, owner.OwnerId);
            ApplyRiserList();
        }
        catch (Exception ex)
        {
            _loadedRisers = Array.Empty<OwnedRiser>();
            _riserLoadError = $"Could not load risers: {ex.Message}";
            _siteRisersBindingSource.DataSource = Array.Empty<OwnedRiser>();
            _siteRisersGrid.Visible = false;
            ShowSiteRisersStatus(_riserLoadError, Color.Firebrick);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnSiteRisersGridCellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        if (_siteRisersGrid.Rows[e.RowIndex].DataBoundItem is not OwnedRiser riser)
        {
            return;
        }

        var owner = _flowController.CurrentOwner;
        var site = _flowController.SelectedSite;
        if (owner is null || site is null)
        {
            return;
        }

        var columnName = _siteRisersGrid.Columns[e.ColumnIndex].Name;
        if (columnName == "RiserName")
        {
            _flowController.ShowRiserDetails(riser);
            ApplyState();
            await ReloadRiserRoomsAsync();
            return;
        }

        if (columnName == "EditRiser")
        {
            using var dialog = BuildEditRiserDialog(owner.OwnerId, site.SiteId, riser);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            await ReloadSiteRisersAsync();
            return;
        }

        if (columnName != "DeleteRiser")
        {
            return;
        }

        if (MessageBox.Show(
                this,
                $"Delete riser '{riser.RiserName}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            await _authService.DeleteRiserAsync(riser.RiserId, site.SiteId, owner.OwnerId);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Delete Riser", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        await ReloadSiteRisersAsync();
    }
}
