namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private Form BuildAddSiteDialog(int ownerId)
    {
        return BuildNameDialog(
            "Add Site",
            "Site Name",
            "Create",
            string.Empty,
            "Enter a site name.",
            siteName => _authService.CreateSiteAsync(ownerId, siteName));
    }

    private Form BuildEditSiteDialog(int ownerId, OwnedSite site)
    {
        return BuildNameDialog(
            "Edit Site",
            "Site Name",
            "Update",
            site.SiteName,
            "Enter a site name.",
            siteName => _authService.UpdateSiteAsync(site.SiteId, ownerId, siteName));
    }

    private Form BuildAddRiserDialog(int ownerId, int siteId)
    {
        return BuildNameDialog(
            "Add Riser",
            "Riser Name",
            "Create",
            string.Empty,
            "Enter a riser name.",
            riserName => _authService.CreateRiserAsync(siteId, ownerId, riserName));
    }

    private Form BuildEditRiserDialog(int ownerId, int siteId, OwnedRiser riser)
    {
        return BuildNameDialog(
            "Edit Riser",
            "Riser Name",
            "Update",
            riser.RiserName,
            "Enter a riser name.",
            riserName => _authService.UpdateRiserAsync(riser.RiserId, siteId, ownerId, riserName));
    }

    private Form BuildAddRoomDialog(int ownerId, int siteId, int riserId)
    {
        return BuildNameDialog(
            "Add Room",
            "Room Name",
            "Create",
            string.Empty,
            "Enter a room name.",
            roomName => _authService.CreateRoomAsync(riserId, siteId, ownerId, roomName));
    }

    private Form BuildEditRoomDialog(int ownerId, int siteId, OwnedRoom room)
    {
        return BuildNameDialog(
            "Update Room",
            "Room Name",
            "Update",
            room.RoomName,
            "Enter a room name.",
            roomName => _authService.UpdateRoomAsync(room.RoomId, siteId, ownerId, roomName));
    }

    private Form BuildNameDialog(
        string title,
        string labelText,
        string submitText,
        string initialValue,
        string emptyValueMessage,
        Func<string, Task> submitAsync)
    {
        var nameLabel = new Label
        {
            AutoSize = true,
            Text = labelText,
            Anchor = AnchorStyles.Left
        };

        var nameTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = initialValue
        };

        var errorLabel = new Label
        {
            AutoSize = true,
            ForeColor = Color.Firebrick,
            MaximumSize = new Size(320, 0)
        };

        var submitButton = new Button
        {
            AutoSize = true,
            Text = submitText,
            Padding = new Padding(10, 6, 10, 6)
        };

        var cancelButton = new Button
        {
            AutoSize = true,
            Text = "Cancel",
            Padding = new Padding(10, 6, 10, 6),
            DialogResult = DialogResult.Cancel
        };

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 8, 0, 0)
        };
        buttons.Controls.Add(submitButton);
        buttons.Controls.Add(cancelButton);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(18)
        };
        content.Controls.Add(nameLabel, 0, 0);
        content.Controls.Add(nameTextBox, 0, 1);
        content.Controls.Add(errorLabel, 0, 2);
        content.Controls.Add(buttons, 0, 3);

        var dialog = new Form
        {
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(360, 150)
        };
        dialog.Controls.Add(content);
        dialog.AcceptButton = submitButton;
        dialog.CancelButton = cancelButton;

        submitButton.Click += async (_, _) =>
        {
            var name = nameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                errorLabel.Text = emptyValueMessage;
                return;
            }

            submitButton.Enabled = false;
            cancelButton.Enabled = false;
            errorLabel.Text = string.Empty;

            try
            {
                await submitAsync(name);
                dialog.DialogResult = DialogResult.OK;
                dialog.Close();
            }
            catch (Exception ex)
            {
                errorLabel.Text = ex.Message;
                submitButton.Enabled = true;
                cancelButton.Enabled = true;
            }
        };

        return dialog;
    }
}
