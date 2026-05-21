using edp_gui_app;

namespace edp_gui_app.Tests;

[TestClass]
public sealed class LoginFlowControllerTests
{
    [TestMethod]
    public void NewController_StartsOnLanding()
    {
        var controller = new LoginFlowController();

        Assert.AreEqual(LoginViewState.Landing, controller.CurrentState);
        Assert.IsNull(controller.CurrentOwner);
        Assert.IsNull(controller.SelectedSite);
    }

    [TestMethod]
    public void ShowLogin_MovesToLoginState()
    {
        var controller = new LoginFlowController();

        controller.ShowLogin();

        Assert.AreEqual(LoginViewState.Login, controller.CurrentState);
    }

    [TestMethod]
    public void ShowSignUp_MovesToSignUpState()
    {
        var controller = new LoginFlowController();

        controller.ShowSignUp();

        Assert.AreEqual(LoginViewState.SignUp, controller.CurrentState);
    }

    [TestMethod]
    public void ShowForgotPassword_MovesToForgotPasswordState()
    {
        var controller = new LoginFlowController();

        controller.ShowForgotPassword();

        Assert.AreEqual(LoginViewState.ForgotPassword, controller.CurrentState);
    }

    [TestMethod]
    public void ShowResetPassword_MovesToResetPasswordState()
    {
        var controller = new LoginFlowController();

        controller.ShowResetPassword();

        Assert.AreEqual(LoginViewState.ResetPassword, controller.CurrentState);
    }

    [TestMethod]
    public void ShowProfile_MovesToProfileState()
    {
        var controller = new LoginFlowController();

        controller.ShowProfile();

        Assert.AreEqual(LoginViewState.Profile, controller.CurrentState);
    }

    [TestMethod]
    public void ShowProfile_StoresOwnerAndMovesToProfileState()
    {
        var controller = new LoginFlowController();
        var owner = new SiteOwner(7, "Maria Santos", "maria@example.com");

        controller.ShowProfile(owner);

        Assert.AreEqual(LoginViewState.Profile, controller.CurrentState);
        Assert.AreEqual(owner, controller.CurrentOwner);
    }

    [TestMethod]
    public void ShowLanding_FromLogin_ReturnsToLanding()
    {
        var controller = new LoginFlowController();
        controller.ShowLogin();

        controller.ShowLanding();

        Assert.AreEqual(LoginViewState.Landing, controller.CurrentState);
        Assert.IsNull(controller.CurrentOwner);
        Assert.IsNull(controller.SelectedSite);
    }

    [TestMethod]
    public void ShowLanding_FromSignUp_ReturnsToLanding()
    {
        var controller = new LoginFlowController();
        controller.ShowSignUp();

        controller.ShowLanding();

        Assert.AreEqual(LoginViewState.Landing, controller.CurrentState);
        Assert.IsNull(controller.CurrentOwner);
        Assert.IsNull(controller.SelectedSite);
    }

    [TestMethod]
    public void ShowOwnerSites_StoresOwnerAndMovesToOwnerSites()
    {
        var controller = new LoginFlowController();
        var owner = new SiteOwner(7, "Maria Santos", "maria@example.com");

        controller.ShowOwnerSites(owner);

        Assert.AreEqual(LoginViewState.OwnerSites, controller.CurrentState);
        Assert.AreEqual(owner, controller.CurrentOwner);
        Assert.IsNull(controller.SelectedSite);
    }

    [TestMethod]
    public void ShowSiteDetails_StoresSelectedSiteAndMovesToSiteDetails()
    {
        var controller = new LoginFlowController();
        var owner = new SiteOwner(7, "Maria Santos", "maria@example.com");
        var site = new OwnedSite(18, "North Tower");
        controller.ShowOwnerSites(owner);

        controller.ShowSiteDetails(site);

        Assert.AreEqual(LoginViewState.SiteDetails, controller.CurrentState);
        Assert.AreEqual(owner, controller.CurrentOwner);
        Assert.AreEqual(site, controller.SelectedSite);
    }

    [TestMethod]
    public void ShowOwnerSites_FromSiteDetails_ReturnsToOwnerSites()
    {
        var controller = new LoginFlowController();
        var owner = new SiteOwner(7, "Maria Santos", "maria@example.com");
        controller.ShowOwnerSites(owner);
        controller.ShowSiteDetails(new OwnedSite(18, "North Tower"));

        controller.ShowOwnerSites();

        Assert.AreEqual(LoginViewState.OwnerSites, controller.CurrentState);
        Assert.AreEqual(owner, controller.CurrentOwner);
        Assert.IsNull(controller.SelectedSite);
    }

    [TestMethod]
    public void Logout_ReturnsToLandingAndClearsOwnerWorkspaceState()
    {
        var controller = new LoginFlowController();
        controller.ShowOwnerSites(new SiteOwner(7, "Maria Santos", "maria@example.com"));
        controller.ShowSiteDetails(new OwnedSite(18, "North Tower"));

        controller.Logout();

        Assert.AreEqual(LoginViewState.Landing, controller.CurrentState);
        Assert.IsNull(controller.CurrentOwner);
        Assert.IsNull(controller.SelectedSite);
    }
}
