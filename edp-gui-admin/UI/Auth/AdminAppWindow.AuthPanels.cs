namespace edp_gui_admin;

public sealed partial class AdminAppWindow
{
    private (Control Panel, TextBox Email, TextBox Password, Label Status) BuildLoginPanel()
    {
        var email = new TextBox { Width = 340, PlaceholderText = "admin" };
        var password = new TextBox { Width = 340, UseSystemPasswordChar = true };
        var submit = BuildActionButton("Login", OnLoginClicked);
        var status = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            ForeColor = Color.Firebrick
        };

        var panel = BuildCardPanel("Admin Login", "Sign in with the admin account to manage global records.");
        panel.Controls.Add(new Label { AutoSize = true, Text = "Username" });
        panel.Controls.Add(email);
        panel.Controls.Add(new Label { AutoSize = true, Text = "Password", Margin = new Padding(0, 10, 0, 0) });
        panel.Controls.Add(password);
        panel.Controls.Add(submit);
        panel.Controls.Add(status);

        return (panel, email, password, status);
    }
}
