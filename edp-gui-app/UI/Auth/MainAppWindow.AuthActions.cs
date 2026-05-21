namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private async void OnSubmitLoginClicked(object? sender, EventArgs e)
    {
        var email = _emailTextBox.Text.Trim();
        var password = _passwordTextBox.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowLoginStatus("Email and password are required.", Color.Firebrick);
            return;
        }

        try
        {
            SetBusy(true);
            ShowLoginStatus("Checking credentials...", Color.DimGray);

            var owner = await _authService.AuthenticateAsync(email, password);
            if (owner is null)
            {
                ShowLoginStatus("Invalid email or password.", Color.Firebrick);
                return;
            }

            await EnterOwnerWorkspaceAsync(owner);
        }
        catch (Exception ex)
        {
            ShowLoginStatus($"Login failed: {ex.Message}", Color.Firebrick);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnSubmitSignUpClicked(object? sender, EventArgs e)
    {
        var ownerName = _signUpNameTextBox.Text.Trim();
        var email = _signUpEmailTextBox.Text.Trim();
        var password = _signUpPasswordTextBox.Text;

        if (string.IsNullOrWhiteSpace(ownerName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            ShowSignUpStatus("Name, email, and password are required.", Color.Firebrick);
            return;
        }

        try
        {
            SetBusy(true);
            ShowSignUpStatus("Creating account...", Color.DimGray);

            var result = await _authService.CreateOwnerAsync(ownerName, email, password);
            if (result.Status == CreateSiteOwnerStatus.EmailAlreadyExists)
            {
                ShowSignUpStatus("Email already exists.", Color.Firebrick);
                return;
            }

            await EnterOwnerWorkspaceAsync(result.Owner!);
        }
        catch (Exception ex)
        {
            ShowSignUpStatus($"Sign-up failed: {ex.Message}", Color.Firebrick);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ResetAuthForms()
    {
        ResetLoginForm();
        ResetSignUpForm();
        ResetPasswordRecoveryForms();
    }

    private void ResetLoginForm()
    {
        _emailTextBox.Clear();
        _passwordTextBox.Clear();
        ResetLoginStatus();
    }

    private void ResetSignUpForm()
    {
        _signUpNameTextBox.Clear();
        _signUpEmailTextBox.Clear();
        _signUpPasswordTextBox.Clear();
        ResetSignUpStatus();
    }

    private void ResetLoginStatus()
    {
        ShowLoginStatus(string.Empty, Color.Firebrick);
    }

    private void ResetSignUpStatus()
    {
        ShowSignUpStatus(string.Empty, Color.Firebrick);
    }

    private void ShowLoginStatus(string text, Color color)
    {
        _loginStatusLabel.Text = text;
        _loginStatusLabel.ForeColor = color;
    }

    private void ShowSignUpStatus(string text, Color color)
    {
        _signUpStatusLabel.Text = text;
        _signUpStatusLabel.ForeColor = color;
    }

}
