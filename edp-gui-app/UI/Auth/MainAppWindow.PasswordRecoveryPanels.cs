namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private (Control Panel, TextBox Email, Button Submit, Label Status) BuildForgotPasswordPanel()
    {
        const string resetLinkText = "Already have a code? Reset password";
        var email = new TextBox { Width = 320, PlaceholderText = "owner@example.com" };
        var submit = new Button { AutoSize = true, Text = "Send Code", Padding = new Padding(10, 6, 10, 6) };
        submit.Click += OnSubmitForgotPasswordClicked;

        var back = new Button { AutoSize = true, Text = "Back" };
        back.Click += (_, _) => ShowLoginView();

        var resetLink = new LinkLabel
        {
            AutoSize = true,
            Text = resetLinkText,
            LinkArea = new LinkArea("Already have a code? ".Length, "Reset password".Length)
        };
        resetLink.LinkClicked += (_, _) => ShowResetPasswordView(email.Text.Trim());

        var status = BuildRecoveryStatusLabel();
        var actions = BuildRecoveryActions(back, submit);

        var panel = BuildCardPanel("Password Recovery", "Enter the email for your site owner account.");
        panel.Controls.Add(new Label { AutoSize = true, Text = "Email" });
        panel.Controls.Add(email);
        panel.Controls.Add(actions);
        panel.Controls.Add(resetLink);
        panel.Controls.Add(status);

        return (panel, email, submit, status);
    }

    private (
        Control Panel,
        Label EmailLabel,
        TextBox Email,
        Label CodeLabel,
        TextBox Code,
        Label NewPasswordLabel,
        TextBox NewPassword,
        Label ConfirmPasswordLabel,
        TextBox ConfirmPassword,
        Button Submit,
        Label Status) BuildResetPasswordPanel()
    {
        var emailLabel = new Label { AutoSize = true, Text = "Email" };
        var email = new TextBox { Width = 320, PlaceholderText = "owner@example.com" };
        var codeLabel = new Label { AutoSize = true, Text = "Reset Code", Margin = new Padding(0, 10, 0, 0) };
        var code = new TextBox { Width = 160, PlaceholderText = "123456" };
        var newPasswordLabel = new Label { AutoSize = true, Text = "New Password", Margin = new Padding(0, 10, 0, 0) };
        var newPassword = new TextBox { Width = 320, UseSystemPasswordChar = true };
        var confirmPasswordLabel = new Label { AutoSize = true, Text = "Confirm Password", Margin = new Padding(0, 10, 0, 0) };
        var confirmPassword = new TextBox { Width = 320, UseSystemPasswordChar = true };
        var submit = new Button { AutoSize = true, Text = "Submit Code", Padding = new Padding(10, 6, 10, 6) };
        submit.Click += OnSubmitResetPasswordClicked;

        var back = new Button { AutoSize = true, Text = "Back" };
        back.Click += (_, _) => ShowLoginView();

        var status = BuildRecoveryStatusLabel();
        var actions = BuildRecoveryActions(back, submit);

        var panel = BuildCardPanel("Reset Password", "Enter the reset code and choose a new password.");
        panel.Controls.Add(emailLabel);
        panel.Controls.Add(email);
        panel.Controls.Add(codeLabel);
        panel.Controls.Add(code);
        panel.Controls.Add(newPasswordLabel);
        panel.Controls.Add(newPassword);
        panel.Controls.Add(confirmPasswordLabel);
        panel.Controls.Add(confirmPassword);
        panel.Controls.Add(actions);
        panel.Controls.Add(status);

        return (panel, emailLabel, email, codeLabel, code, newPasswordLabel, newPassword, confirmPasswordLabel,
            confirmPassword, submit, status);
    }

    private static Label BuildRecoveryStatusLabel()
    {
        return new Label
        {
            AutoSize = true,
            MaximumSize = new Size(420, 0),
            ForeColor = Color.Firebrick
        };
    }

    private static FlowLayoutPanel BuildRecoveryActions(params Control[] controls)
    {
        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        actions.Controls.AddRange(controls);
        return actions;
    }
}
