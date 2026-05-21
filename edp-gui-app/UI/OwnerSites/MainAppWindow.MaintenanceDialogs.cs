namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private Form BuildMaintenanceDialog(int ownerId, IReadOnlyList<OwnedSite> sites)
    {
        var siteCombo = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            DataSource = sites.ToArray(),
            DisplayMember = nameof(OwnedSite.SiteName),
            ValueMember = nameof(OwnedSite.SiteId)
        };
        var titleTextBox = new TextBox { Dock = DockStyle.Fill };
        var descriptionTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 80 };
        var priorityCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        priorityCombo.Items.AddRange(["Low", "Normal", "High", "Urgent"]);
        priorityCombo.SelectedItem = "Normal";

        var errorLabel = new Label { AutoSize = true, ForeColor = Color.Firebrick, MaximumSize = new Size(380, 0) };
        var submitButton = new Button { AutoSize = true, Text = "Create", Padding = new Padding(10, 6, 10, 6) };
        var cancelButton = new Button
        {
            AutoSize = true,
            Text = "Cancel",
            Padding = new Padding(10, 6, 10, 6),
            DialogResult = DialogResult.Cancel
        };

        var content = BuildMaintenanceDialogLayout(siteCombo, titleTextBox, descriptionTextBox, priorityCombo);
        var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        buttons.Controls.Add(submitButton);
        buttons.Controls.Add(cancelButton);
        content.Controls.Add(errorLabel, 0, 8);
        content.Controls.Add(buttons, 0, 9);

        var dialog = new Form
        {
            Text = "Add Maintenance Request",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(430, 360)
        };
        dialog.Controls.Add(content);
        dialog.AcceptButton = submitButton;
        dialog.CancelButton = cancelButton;

        submitButton.Click += async (_, _) => await SubmitMaintenanceDialogAsync(
            dialog, siteCombo, titleTextBox, descriptionTextBox, priorityCombo, errorLabel, ownerId);
        return dialog;
    }

    private static TableLayoutPanel BuildMaintenanceDialogLayout(params Control[] controls)
    {
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 10,
            Padding = new Padding(18)
        };
        var labels = new[] { "Site", "Title", "Description", "Priority" };
        for (var index = 0; index < labels.Length; index++)
        {
            content.Controls.Add(new Label { AutoSize = true, Text = labels[index] }, 0, index * 2);
            content.Controls.Add(controls[index], 0, index * 2 + 1);
        }

        return content;
    }

    private async Task SubmitMaintenanceDialogAsync(
        Form dialog,
        ComboBox siteCombo,
        TextBox titleTextBox,
        TextBox descriptionTextBox,
        ComboBox priorityCombo,
        Label errorLabel,
        int ownerId)
    {
        if (_maintenanceService is null || siteCombo.SelectedItem is not OwnedSite site)
        {
            errorLabel.Text = "Select a site.";
            return;
        }

        var title = titleTextBox.Text.Trim();
        var description = descriptionTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
        {
            errorLabel.Text = "Enter a title and description.";
            return;
        }

        try
        {
            await _maintenanceService.CreateMaintenanceTicketAsync(
                ownerId,
                site.SiteId,
                null,
                null,
                title,
                description,
                Convert.ToString(priorityCombo.SelectedItem) ?? "Normal");
            dialog.DialogResult = DialogResult.OK;
            dialog.Close();
        }
        catch (Exception ex)
        {
            errorLabel.Text = ex.Message;
        }
    }
}
