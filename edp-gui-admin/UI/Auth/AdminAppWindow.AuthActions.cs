namespace edp_gui_admin;

public sealed partial class AdminAppWindow
{
    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        _loginStatusLabel.Text = string.Empty;
        _loginStatusLabel.ForeColor = Color.Firebrick;
        var username = _emailTextBox.Text.Trim();
        var password = _passwordTextBox.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _loginStatusLabel.Text = "Enter admin username and password.";
            return;
        }

        try
        {
            await _authService.EnsureSchemaAsync();
            var admin = await _authService.AuthenticateAsync(username, password);
            if (admin is null)
            {
                _loginStatusLabel.Text = "Invalid admin credentials.";
                return;
            }

            await ShowWorkspaceAsync();
        }
        catch (Exception ex)
        {
            _loginStatusLabel.Text = ex.Message;
        }
    }
}
