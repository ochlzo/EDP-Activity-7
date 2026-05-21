namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private async void OnSubmitForgotPasswordClicked(object? sender, EventArgs e)
    {
        var email = _forgotPasswordEmailTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            ShowForgotPasswordStatus("Email is required.", Color.Firebrick);
            return;
        }

        try
        {
            SetBusy(true);
            ShowForgotPasswordStatus("Sending reset code...", Color.DimGray);

            var status = await _authService.RequestPasswordResetAsync(email);
            if (status == PasswordResetRequestStatus.EmailNotConfigured)
            {
                ShowForgotPasswordStatus("Password reset email is not configured.", Color.Firebrick);
                return;
            }

            if (status == PasswordResetRequestStatus.EmailDoesNotExist)
            {
                ShowForgotPasswordStatus("Email does not exist.", Color.Firebrick);
                return;
            }

            ShowResetPasswordView(email);
            ShowResetPasswordStatus("If the email exists, a reset code was sent.", Color.DimGray);
        }
        catch (Exception ex)
        {
            ShowForgotPasswordStatus($"Password reset failed: {ex.Message}", Color.Firebrick);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnSubmitResetPasswordClicked(object? sender, EventArgs e)
    {
        var email = _resetPasswordEmailTextBox.Text.Trim();
        var code = _resetPasswordCodeTextBox.Text.Trim();
        var newPassword = _resetPasswordNewPasswordTextBox.Text;
        var confirmPassword = _resetPasswordConfirmPasswordTextBox.Text;

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(code))
        {
            ShowResetPasswordStatus("Email and reset code are required.", Color.Firebrick);
            return;
        }

        if (!_resetPasswordCodeVerified)
        {
            await VerifyResetCodeAsync(email, code);
            return;
        }

        if (string.IsNullOrWhiteSpace(newPassword) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            ShowResetPasswordStatus("New password and confirmation are required.", Color.Firebrick);
            return;
        }

        if (newPassword != confirmPassword)
        {
            ShowResetPasswordStatus("Passwords do not match.", Color.Firebrick);
            return;
        }

        await ResetPasswordAndLoginAsync(email, code, newPassword);
    }

    private async Task VerifyResetCodeAsync(string email, string code)
    {
        try
        {
            SetBusy(true);
            ShowResetPasswordStatus("Checking reset code...", Color.DimGray);

            var status = await _authService.VerifyPasswordResetCodeAsync(email, code);
            if (status == PasswordResetCodeStatus.InvalidOrExpired)
            {
                ShowResetPasswordStatus("Invalid or expired reset code.", Color.Firebrick);
                return;
            }

            SetResetPasswordStage(true);
            ShowResetPasswordStatus("Code verified. Enter a new password.", Color.DimGray);
            _resetPasswordNewPasswordTextBox.Focus();
        }
        catch (Exception ex)
        {
            ShowResetPasswordStatus($"Password reset failed: {ex.Message}", Color.Firebrick);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task ResetPasswordAndLoginAsync(string email, string code, string newPassword)
    {
        try
        {
            SetBusy(true);
            ShowResetPasswordStatus("Resetting password...", Color.DimGray);

            var status = await _authService.ResetPasswordAsync(email, code, newPassword);
            if (status == PasswordResetStatus.InvalidOrExpired)
            {
                ShowResetPasswordStatus("Invalid or expired reset code.", Color.Firebrick);
                SetResetPasswordStage(false);
                return;
            }

            var owner = await _authService.AuthenticateAsync(email, newPassword);
            if (owner is null)
            {
                ShowResetPasswordStatus("Password reset succeeded, but login failed.", Color.Firebrick);
                return;
            }

            ResetPasswordRecoveryForms();
            await EnterOwnerWorkspaceAsync(owner);
        }
        catch (Exception ex)
        {
            ShowResetPasswordStatus($"Password reset failed: {ex.Message}", Color.Firebrick);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ResetPasswordRecoveryForms()
    {
        _resetPasswordCodeVerified = false;
        _forgotPasswordEmailTextBox.Clear();
        _resetPasswordEmailTextBox.Clear();
        _resetPasswordCodeTextBox.Clear();
        _resetPasswordNewPasswordTextBox.Clear();
        _resetPasswordConfirmPasswordTextBox.Clear();
        SetResetPasswordStage(false);
        ResetForgotPasswordStatus();
        ResetResetPasswordStatus();
    }

    private void SetResetPasswordStage(bool codeVerified)
    {
        _resetPasswordCodeVerified = codeVerified;
        _resetPasswordEmailLabel.Visible = !codeVerified;
        _resetPasswordEmailTextBox.Visible = !codeVerified;
        _resetPasswordCodeLabel.Visible = !codeVerified;
        _resetPasswordCodeTextBox.Visible = !codeVerified;
        _resetPasswordNewPasswordLabel.Visible = codeVerified;
        _resetPasswordNewPasswordTextBox.Visible = codeVerified;
        _resetPasswordConfirmPasswordLabel.Visible = codeVerified;
        _resetPasswordConfirmPasswordTextBox.Visible = codeVerified;
        _resetPasswordSubmitButton.Text = codeVerified ? "Update Password" : "Submit Code";
    }

    private void ResetForgotPasswordStatus()
    {
        ShowForgotPasswordStatus(string.Empty, Color.Firebrick);
    }

    private void ResetResetPasswordStatus()
    {
        ShowResetPasswordStatus(string.Empty, Color.Firebrick);
    }

    private void ShowForgotPasswordStatus(string text, Color color)
    {
        _forgotPasswordStatusLabel.Text = text;
        _forgotPasswordStatusLabel.ForeColor = color;
    }

    private void ShowResetPasswordStatus(string text, Color color)
    {
        _resetPasswordStatusLabel.Text = text;
        _resetPasswordStatusLabel.ForeColor = color;
    }
}
