namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private void ShowLandingView()
    {
        ResetAuthForms();
        ResetOwnerWorkspace();
        _flowController.Logout();
        ApplyState();
    }

    private void ShowLoginView()
    {
        ResetLoginStatus();
        SetBusy(false);
        _flowController.ShowLogin();
        ApplyState();
        _emailTextBox.Focus();
    }

    private void ShowSignUpView()
    {
        ResetSignUpStatus();
        SetBusy(false);
        _flowController.ShowSignUp();
        ApplyState();
        _signUpNameTextBox.Focus();
    }

    private void ShowForgotPasswordView()
    {
        ResetForgotPasswordStatus();
        SetBusy(false);
        _flowController.ShowForgotPassword();
        ApplyState();
        _forgotPasswordEmailTextBox.Focus();
    }

    private void ShowResetPasswordView(string? email = null)
    {
        ResetResetPasswordStatus();
        SetBusy(false);
        if (!string.IsNullOrWhiteSpace(email))
        {
            _resetPasswordEmailTextBox.Text = email;
        }

        SetResetPasswordStage(false);
        _flowController.ShowResetPassword();
        ApplyState();
        _resetPasswordCodeTextBox.Focus();
    }
}
