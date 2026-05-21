namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private Form BuildAddTenantToRoomDialog(int ownerId, int siteId, OwnedRoom room)
    {
        var fields = new Dictionary<string, TextBox>
        {
            ["Name"] = new(),
            ["Email"] = new(),
            ["Address"] = new(),
            ["Contact Number"] = new()
        };

        return BuildTenantFormDialog(
            "Add Tenant",
            "Create",
            fields,
            async values => await _authService.CreateTenantInRoomAsync(
                room.RoomId,
                siteId,
                ownerId,
                values["Name"],
                values["Email"],
                values["Address"],
                values["Contact Number"]));
    }

    private Form BuildTenantDetailsDialog(
        OwnedTenant tenant,
        IReadOnlyList<OwnedDocumentAttachment> documents,
        Func<Task<bool>> sendRequestAsync)
    {
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 1,
            Padding = new Padding(18)
        };
        content.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Tenant Details",
            Font = new Font(Font.FontFamily, 16, FontStyle.Bold)
        });
        content.Controls.Add(BuildTenantDetailsGrid(tenant));
        content.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Documents",
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(0, 12, 0, 4)
        });
        content.Controls.Add(BuildTenantDocumentsList(documents));

        var status = new Label { AutoSize = true, ForeColor = Color.DimGray, MaximumSize = new Size(430, 0) };
        var edit = new Button { AutoSize = true, Text = "Edit", Padding = new Padding(10, 6, 10, 6) };
        var replace = new Button { AutoSize = true, Text = "Replace Tenant", Padding = new Padding(10, 6, 10, 6) };
        var send = new Button { AutoSize = true, Text = "Send Document Request", Padding = new Padding(10, 6, 10, 6) };
        var close = new Button
        {
            AutoSize = true,
            Text = "Close",
            Padding = new Padding(10, 6, 10, 6),
            DialogResult = DialogResult.Cancel
        };
        var actions = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        actions.Controls.Add(edit);
        actions.Controls.Add(replace);
        actions.Controls.Add(send);
        actions.Controls.Add(close);
        content.Controls.Add(status);
        content.Controls.Add(actions);

        var dialog = new Form
        {
            Text = "Tenant Details",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(520, 430)
        };
        dialog.Controls.Add(content);
        dialog.CancelButton = close;
        edit.Click += (_, _) =>
        {
            dialog.DialogResult = DialogResult.OK;
            dialog.Close();
        };

        replace.Click += (_, _) =>
        {
            dialog.DialogResult = DialogResult.Yes;
            dialog.Close();
        };

        send.Click += async (_, _) =>
        {
            send.Enabled = false;
            status.ForeColor = Color.DimGray;
            status.Text = "Sending request...";
            try
            {
                var sent = await sendRequestAsync();
                if (sent)
                {
                    dialog.DialogResult = DialogResult.Retry;
                    dialog.Close();
                    return;
                }

                status.ForeColor = sent ? Color.DarkGreen : Color.DimGray;
                status.Text = sent ? "Document request email sent." : "Document request canceled.";
            }
            catch (Exception ex)
            {
                status.ForeColor = Color.Firebrick;
                status.Text = ex.Message;
            }
            finally
            {
                send.Enabled = true;
            }
        };

        return dialog;
    }

    private IReadOnlyList<string>? ShowDocumentRequestPicker()
    {
        var checks = DocumentRequestCatalog.RequestableDocuments
            .Select(document => new CheckBox { AutoSize = true, Text = document })
            .ToArray();
        var error = new Label { AutoSize = true, ForeColor = Color.Firebrick };
        var submit = new Button { AutoSize = true, Text = "Send", Padding = new Padding(10, 6, 10, 6) };
        var cancel = new Button
        {
            AutoSize = true,
            Text = "Cancel",
            Padding = new Padding(10, 6, 10, 6),
            DialogResult = DialogResult.Cancel
        };
        var content = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(18)
        };
        content.Controls.Add(new Label { AutoSize = true, Text = "Select documents to request." });
        content.Controls.AddRange(checks);
        content.Controls.Add(error);
        var actions = new FlowLayoutPanel { AutoSize = true };
        actions.Controls.Add(submit);
        actions.Controls.Add(cancel);
        content.Controls.Add(actions);

        using var dialog = new Form
        {
            Text = "Send Document Request",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(360, 300)
        };
        dialog.Controls.Add(content);
        dialog.CancelButton = cancel;
        submit.Click += (_, _) =>
        {
            if (checks.Any(check => check.Checked))
            {
                dialog.DialogResult = DialogResult.OK;
                dialog.Close();
                return;
            }

            error.Text = "Select at least one document.";
        };

        return dialog.ShowDialog(this) == DialogResult.OK
            ? checks.Where(check => check.Checked).Select(check => check.Text).ToArray()
            : null;
    }

    private Form BuildTenantFormDialog(
        string title,
        string submitText,
        Dictionary<string, TextBox> fields,
        Func<IReadOnlyDictionary<string, string>, Task> submitAsync)
    {
        foreach (var field in fields.Values)
        {
            field.Dock = DockStyle.Fill;
        }

        var error = new Label { AutoSize = true, ForeColor = Color.Firebrick, MaximumSize = new Size(360, 0) };
        var submit = new Button { AutoSize = true, Text = submitText, Padding = new Padding(10, 6, 10, 6) };
        var cancel = new Button
        {
            AutoSize = true,
            Text = "Cancel",
            Padding = new Padding(10, 6, 10, 6),
            DialogResult = DialogResult.Cancel
        };
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(18)
        };

        foreach (var (label, field) in fields)
        {
            content.Controls.Add(new Label { AutoSize = true, Text = label });
            content.Controls.Add(field);
        }

        var actions = new FlowLayoutPanel { AutoSize = true };
        actions.Controls.Add(submit);
        actions.Controls.Add(cancel);
        content.Controls.Add(error);
        content.Controls.Add(actions);

        var dialog = new Form
        {
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(420, 300)
        };
        dialog.Controls.Add(content);
        dialog.AcceptButton = submit;
        dialog.CancelButton = cancel;

        submit.Click += async (_, _) =>
        {
            var values = fields.ToDictionary(item => item.Key, item => item.Value.Text.Trim());
            if (string.IsNullOrWhiteSpace(values["Name"]) || string.IsNullOrWhiteSpace(values["Email"]))
            {
                error.Text = "Name and email are required.";
                return;
            }

            submit.Enabled = false;
            cancel.Enabled = false;
            error.Text = string.Empty;
            try
            {
                await submitAsync(values);
                dialog.DialogResult = DialogResult.OK;
                dialog.Close();
            }
            catch (Exception ex)
            {
                error.Text = ex.Message;
                submit.Enabled = true;
                cancel.Enabled = true;
            }
        };

        return dialog;
    }

    private Control BuildTenantDetailsGrid(OwnedTenant tenant)
    {
        var grid = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 10, 0, 0) };
        AddTenantDetail(grid, "Name", tenant.Name);
        AddTenantDetail(grid, "Email", tenant.Email);
        AddTenantDetail(grid, "Address", tenant.Address);
        AddTenantDetail(grid, "Contact Number", tenant.ContactNumber);
        return grid;
    }

    private static void AddTenantDetail(TableLayoutPanel grid, string label, string value)
    {
        grid.Controls.Add(new Label { AutoSize = true, Text = label, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold) });
        grid.Controls.Add(new Label { AutoSize = true, Text = string.IsNullOrWhiteSpace(value) ? "-" : value });
    }
}
