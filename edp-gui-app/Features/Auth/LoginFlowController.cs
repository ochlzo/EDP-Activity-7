namespace edp_gui_app;

public sealed class LoginFlowController
{
    public LoginViewState CurrentState { get; private set; } = LoginViewState.Landing;

    public SiteOwner? CurrentOwner { get; private set; }
    public OwnedSite? SelectedSite { get; private set; }
    public OwnedRiser? SelectedRiser { get; private set; }

    public void ShowLanding()
    {
        CurrentState = LoginViewState.Landing;
        CurrentOwner = null;
        SelectedSite = null;
        SelectedRiser = null;
    }

    public void ShowLogin()
    {
        CurrentState = LoginViewState.Login;
        CurrentOwner = null;
        SelectedSite = null;
        SelectedRiser = null;
    }

    public void ShowSignUp()
    {
        CurrentState = LoginViewState.SignUp;
        CurrentOwner = null;
        SelectedSite = null;
        SelectedRiser = null;
    }

    public void ShowOwnerSites(SiteOwner owner)
    {
        CurrentOwner = owner;
        SelectedSite = null;
        SelectedRiser = null;
        CurrentState = LoginViewState.OwnerSites;
    }

    public void ShowOwnerSites()
    {
        SelectedSite = null;
        SelectedRiser = null;
        CurrentState = LoginViewState.OwnerSites;
    }

    public void ShowSiteDetails(OwnedSite site)
    {
        SelectedSite = site;
        SelectedRiser = null;
        CurrentState = LoginViewState.SiteDetails;
    }

    public void ShowSiteDetails()
    {
        SelectedRiser = null;
        CurrentState = LoginViewState.SiteDetails;
    }

    public void ShowRiserDetails(OwnedRiser riser)
    {
        SelectedRiser = riser;
        CurrentState = LoginViewState.RiserDetails;
    }

    public void Logout()
    {
        ShowLanding();
    }
}
