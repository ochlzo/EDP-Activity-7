namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private readonly BindingSource _sitesBindingSource = new();
    private IReadOnlyList<OwnedSite> _loadedSites = Array.Empty<OwnedSite>();
    private string? _siteLoadError;

    private void ResetOwnerWorkspace()
    {
        _loadedSites = Array.Empty<OwnedSite>();
        _siteLoadError = null;
        _sitesBindingSource.DataSource = Array.Empty<OwnedSite>();
        _sitesGrid.Visible = false;
        _siteSearchTextBox.Clear();
        ShowOwnerSitesStatus(string.Empty, Color.DimGray);
        ResetProfileState();
        ResetMaintenanceState();
        ResetSiteDetailsState();
        ResetRiserDetailsState();
    }

    private void ApplyOwnedSiteFilter()
    {
        if (_siteLoadError is not null)
        {
            return;
        }

        var filteredSites = OwnedSiteFilter.Apply(_loadedSites, _siteSearchTextBox.Text).ToArray();
        _sitesBindingSource.DataSource = filteredSites;
        _sitesGrid.Visible = filteredSites.Length > 0;

        if (filteredSites.Length > 0)
        {
            ShowOwnerSitesStatus(string.Empty, Color.DimGray);
            return;
        }

        var message = _loadedSites.Count == 0
            ? "No sites are assigned to this owner yet."
            : "No matching sites found.";
        ShowOwnerSitesStatus(message, Color.DimGray);
    }

    private void ShowOwnerSitesStatus(string text, Color color)
    {
        _ownerSitesStatusLabel.Text = text;
        _ownerSitesStatusLabel.ForeColor = color;
    }
}
