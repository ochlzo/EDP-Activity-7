namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private readonly BindingSource _siteRisersBindingSource = new();
    private IReadOnlyList<OwnedRiser> _loadedRisers = Array.Empty<OwnedRiser>();
    private string? _riserLoadError;

    private void ResetSiteDetailsState()
    {
        _loadedRisers = Array.Empty<OwnedRiser>();
        _riserLoadError = null;
        _siteRisersBindingSource.DataSource = Array.Empty<OwnedRiser>();
        _siteRisersGrid.Visible = false;
        ShowSiteRisersStatus(string.Empty, Color.DimGray);
        _siteDetailsIdValueLabel.Text = "-";
        _siteDetailsNameValueLabel.Text = "-";
    }

    private void UpdateSiteDetailsPanel()
    {
        var site = _flowController.SelectedSite;
        _siteDetailsIdValueLabel.Text = site?.SiteId.ToString() ?? "-";
        _siteDetailsNameValueLabel.Text = site?.SiteName ?? "-";

        if (site is not null)
        {
            return;
        }

        _loadedRisers = Array.Empty<OwnedRiser>();
        _riserLoadError = null;
        _siteRisersBindingSource.DataSource = Array.Empty<OwnedRiser>();
        _siteRisersGrid.Visible = false;
        ShowSiteRisersStatus(string.Empty, Color.DimGray);
    }

    private void ApplyRiserList()
    {
        if (_riserLoadError is not null)
        {
            return;
        }

        var risers = _loadedRisers.ToArray();
        _siteRisersBindingSource.DataSource = risers;
        _siteRisersGrid.Visible = risers.Length > 0;

        var message = risers.Length == 0
            ? "No risers are assigned to this site yet."
            : string.Empty;
        ShowSiteRisersStatus(message, Color.DimGray);
    }

    private void ShowSiteRisersStatus(string text, Color color)
    {
        _siteRisersStatusLabel.Text = text;
        _siteRisersStatusLabel.ForeColor = color;
    }
}
