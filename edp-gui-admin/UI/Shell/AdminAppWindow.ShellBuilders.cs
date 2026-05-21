namespace edp_gui_admin;

public sealed partial class AdminAppWindow
{
    private static FlowLayoutPanel BuildCardPanel(string heading, string description)
    {
        var headingLabel = new Label
        {
            AutoSize = true,
            Text = heading,
            Font = new Font(FontFamily.GenericSansSerif, 20, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 10)
        };

        var descriptionLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            Text = description,
            Margin = new Padding(0, 0, 0, 18)
        };

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.White,
            Padding = new Padding(26)
        };
        panel.Controls.Add(headingLabel);
        panel.Controls.Add(descriptionLabel);
        return panel;
    }

    private static Button BuildActionButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            AutoSize = true,
            Text = text,
            Padding = new Padding(10, 6, 10, 6)
        };
        button.Click += onClick;
        return button;
    }
}
