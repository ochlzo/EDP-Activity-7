namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private (
        Control Panel,
        TextBox Name,
        TextBox Email,
        TextBox ContactNumber,
        TextBox CurrentPassword,
        TextBox NewPassword,
        TextBox ConfirmPassword,
        Button SaveDetails,
        Button ChangeEmail,
        Button ChangePassword,
        Button Back,
        Label Status) BuildProfilePanel()
    {
        var name = new TextBox { Width = 320, PlaceholderText = "Owner name" };
        var email = new TextBox { Width = 320, PlaceholderText = "owner@example.com" };
        var contactNumber = new TextBox { Width = 320, PlaceholderText = "Contact number" };
        var currentPassword = new TextBox { Width = 320, UseSystemPasswordChar = true };
        var newPassword = new TextBox { Width = 320, UseSystemPasswordChar = true };
        var confirmPassword = new TextBox { Width = 320, UseSystemPasswordChar = true };

        var saveDetails = new Button
        {
            AutoSize = true,
            Text = "Save Details",
            Padding = new Padding(10, 6, 10, 6)
        };
        saveDetails.Click += OnSaveProfileDetailsClicked;

        var changeEmail = new Button
        {
            AutoSize = true,
            Text = "Change Email",
            Padding = new Padding(10, 6, 10, 6)
        };
        changeEmail.Click += OnChangeProfileEmailClicked;

        var changePassword = new Button
        {
            AutoSize = true,
            Text = "Change Password",
            Padding = new Padding(10, 6, 10, 6)
        };
        changePassword.Click += OnChangeProfilePasswordClicked;

        var back = new Button
        {
            AutoSize = true,
            Text = "Back to Sites",
            Padding = new Padding(10, 6, 10, 6)
        };
        back.Click += (_, _) => ShowOwnerSitesView();

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        actions.Controls.Add(back);

        var status = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            Margin = new Padding(0, 12, 0, 0)
        };

        var panel = BuildCardPanel("Profile", "Update your account details, email, and password.");
        panel.Controls.Add(BuildSectionLabel("Account Details"));
        panel.Controls.Add(BuildFieldRow("Owner Name", name));
        panel.Controls.Add(BuildFieldRow("Contact Number", contactNumber));
        panel.Controls.Add(saveDetails);
        panel.Controls.Add(BuildSectionLabel("Email"));
        panel.Controls.Add(BuildFieldRow("New Email", email));
        panel.Controls.Add(BuildFieldRow("Current Password", currentPassword, true));
        panel.Controls.Add(changeEmail);
        panel.Controls.Add(BuildSectionLabel("Password"));
        panel.Controls.Add(BuildFieldRow("New Password", newPassword, true));
        panel.Controls.Add(BuildFieldRow("Confirm Password", confirmPassword, true));
        panel.Controls.Add(changePassword);
        panel.Controls.Add(actions);
        panel.Controls.Add(status);

        return (panel, name, email, contactNumber, currentPassword, newPassword, confirmPassword, saveDetails,
            changeEmail, changePassword, back, status);
    }

    private static FlowLayoutPanel BuildFieldRow(string labelText, TextBox textBox, bool password = false)
    {
        var label = new Label
        {
            AutoSize = true,
            Text = labelText
        };

        if (password)
        {
            textBox.UseSystemPasswordChar = true;
        }

        var stack = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 8)
        };
        stack.Controls.Add(label);
        stack.Controls.Add(textBox);
        return stack;
    }

    private static Label BuildSectionLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            Text = text,
            Font = new Font(FontFamily.GenericSansSerif, 11, FontStyle.Bold),
            Margin = new Padding(0, 12, 0, 0)
        };
    }
}
