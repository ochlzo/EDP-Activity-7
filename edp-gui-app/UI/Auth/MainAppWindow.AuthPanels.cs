namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private Control BuildLandingPanel()
    {
        var openLoginButton = new Button
        {
            AutoSize = true,
            Text = "Login",
            Padding = new Padding(10, 6, 10, 6)
        };
        openLoginButton.Click += (_, _) => ShowLoginView();

        var openSignUpButton = new Button
        {
            AutoSize = true,
            Text = "Sign Up",
            Padding = new Padding(10, 6, 10, 6)
        };
        openSignUpButton.Click += (_, _) => ShowSignUpView();

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        actions.Controls.Add(openLoginButton);
        actions.Controls.Add(openSignUpButton);

        var panel = BuildCardPanel("Welcome", "Login or create a site owner account to continue.");
        panel.Controls.Add(actions);
        panel.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(420, 0),
            Text = "The owner workspace will open inside this same desktop window after authentication."
        });

        return panel;
    }

    private (Control Panel, TextBox Email, TextBox Password, Button Submit, Label Status) BuildLoginPanel()
    {
        const string signUpLinkText = "No account? Go to signup";
        var email = new TextBox { Width = 320, PlaceholderText = "owner@example.com" };
        var password = new TextBox { Width = 320, UseSystemPasswordChar = true };
        var submit = new Button { AutoSize = true, Text = "Submit", Padding = new Padding(10, 6, 10, 6) };
        submit.Click += OnSubmitLoginClicked;

        var back = new Button { AutoSize = true, Text = "Back" };
        back.Click += (_, _) => ShowLandingView();

        var signUpLink = new LinkLabel
        {
            AutoSize = true,
            Text = signUpLinkText,
            LinkArea = new LinkArea("No account? ".Length, "Go to signup".Length)
        };
        signUpLink.LinkClicked += (_, _) => ShowSignUpView();

        var forgotPasswordLink = new LinkLabel
        {
            AutoSize = true,
            Text = "Forgot password?",
        };
        forgotPasswordLink.LinkClicked += (_, _) => ShowForgotPasswordView();

        var status = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(420, 0),
            ForeColor = Color.Firebrick
        };

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        actions.Controls.Add(back);
        actions.Controls.Add(submit);

        var panel = BuildCardPanel("Login", "Enter your email and password.");
        panel.Controls.Add(new Label { AutoSize = true, Text = "Email" });
        panel.Controls.Add(email);
        panel.Controls.Add(new Label { AutoSize = true, Text = "Password", Margin = new Padding(0, 10, 0, 0) });
        panel.Controls.Add(password);
        panel.Controls.Add(actions);
        panel.Controls.Add(forgotPasswordLink);
        panel.Controls.Add(signUpLink);
        panel.Controls.Add(status);

        return (panel, email, password, submit, status);
    }

    private (Control Panel, TextBox Name, TextBox Email, TextBox Password, Button Submit, Label Status) BuildSignUpPanel()
    {
        const string loginLinkText = "Already have an account? Go to login";
        var name = new TextBox { Width = 320, PlaceholderText = "Site Owner Name" };
        var email = new TextBox { Width = 320, PlaceholderText = "owner@example.com" };
        var password = new TextBox { Width = 320, UseSystemPasswordChar = true };
        var submit = new Button { AutoSize = true, Text = "Create Account", Padding = new Padding(10, 6, 10, 6) };
        submit.Click += OnSubmitSignUpClicked;

        var back = new Button { AutoSize = true, Text = "Back" };
        back.Click += (_, _) => ShowLandingView();

        var loginLink = new LinkLabel
        {
            AutoSize = true,
            Text = loginLinkText,
            LinkArea = new LinkArea("Already have an account? ".Length, "Go to login".Length)
        };
        loginLink.LinkClicked += (_, _) => ShowLoginView();

        var status = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(420, 0),
            ForeColor = Color.Firebrick
        };

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        actions.Controls.Add(back);
        actions.Controls.Add(submit);

        var panel = BuildCardPanel("Sign Up", "Create a new site owner account.");
        panel.Controls.Add(new Label { AutoSize = true, Text = "Owner Name" });
        panel.Controls.Add(name);
        panel.Controls.Add(new Label { AutoSize = true, Text = "Email", Margin = new Padding(0, 10, 0, 0) });
        panel.Controls.Add(email);
        panel.Controls.Add(new Label { AutoSize = true, Text = "Password", Margin = new Padding(0, 10, 0, 0) });
        panel.Controls.Add(password);
        panel.Controls.Add(actions);
        panel.Controls.Add(loginLink);
        panel.Controls.Add(status);

        return (panel, name, email, password, submit, status);
    }
}
