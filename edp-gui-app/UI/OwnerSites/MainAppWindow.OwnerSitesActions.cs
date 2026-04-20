namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private async Task EnterOwnerWorkspaceAsync(SiteOwner owner)
    {
        _flowController.ShowOwnerSites(owner);
        ResetOwnerWorkspace();
        ApplyState();
        await ReloadOwnedSitesAsync();
    }

    private void ShowOwnerSitesView()
    {
        _flowController.ShowOwnerSites();
        ApplyState();
        ApplyOwnedSiteFilter();
    }

    private async void OnRefreshSitesClicked(object? sender, EventArgs e)
    {
        await ReloadOwnedSitesAsync();
    }

    private async void OnAddSiteClicked(object? sender, EventArgs e)
    {
        var owner = _flowController.CurrentOwner;
        if (owner is null)
        {
            return;
        }

        using var dialog = BuildAddSiteDialog(owner.OwnerId);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        await ReloadOwnedSitesAsync();
    }

    private async Task ReloadOwnedSitesAsync()
    {
        var owner = _flowController.CurrentOwner;
        if (owner is null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            _siteLoadError = null;
            ShowOwnerSitesStatus("Loading sites...", Color.DimGray);

            _loadedSites = await _authService.LoadSitesByOwnerAsync(owner.OwnerId);
            ApplyOwnedSiteFilter();
        }
        catch (Exception ex)
        {
            _loadedSites = Array.Empty<OwnedSite>();
            _siteLoadError = $"Could not load sites: {ex.Message}";
            _sitesBindingSource.DataSource = Array.Empty<OwnedSite>();
            _sitesGrid.Visible = false;
            ShowOwnerSitesStatus(_siteLoadError, Color.Firebrick);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnSitesGridCellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        if (_sitesGrid.Rows[e.RowIndex].DataBoundItem is not OwnedSite site)
        {
            return;
        }

        var columnName = _sitesGrid.Columns[e.ColumnIndex].Name;
        if (columnName == "SiteName")
        {
            _flowController.ShowSiteDetails(site);
            ApplyState();
            await ReloadSiteRisersAsync();
            return;
        }

        if (columnName == "EditSite")
        {
            var currentOwner = _flowController.CurrentOwner;
            if (currentOwner is null)
            {
                return;
            }

            using var dialog = BuildEditSiteDialog(currentOwner.OwnerId, site);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            await ReloadOwnedSitesAsync();
            return;
        }

        if (columnName != "DeleteSite")
        {
            return;
        }

        if (MessageBox.Show(
                this,
                $"Delete site '{site.SiteName}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            return;
        }

        var owner = _flowController.CurrentOwner;
        if (owner is null)
        {
            return;
        }

        try
        {
            await _authService.DeleteSiteAsync(site.SiteId, owner.OwnerId);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Delete Site", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        await ReloadOwnedSitesAsync();
    }
}
