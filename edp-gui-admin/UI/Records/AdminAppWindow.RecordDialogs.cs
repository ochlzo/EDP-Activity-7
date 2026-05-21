namespace edp_gui_admin;

public sealed partial class AdminAppWindow
{
    private sealed record DialogField(
        string Key,
        string Label,
        string Value,
        bool IsPassword,
        IReadOnlyList<DialogOption>? Options)
    {
        public static DialogField Text(string key, string label, string value) =>
            new(key, label, value, false, null);

        public static DialogField Password(string key, string label) =>
            new(key, label, string.Empty, true, null);

        public static DialogField Combo(
            string key,
            string label,
            string value,
            IReadOnlyList<DialogOption> options) =>
            new(key, label, value, false, options);
    }

    private sealed record DialogOption(string Value, string Label);

    private static IReadOnlyDictionary<string, string>? ShowRecordDialog(
        string title,
        params (string Key, string Label, string Value)[] fields)
    {
        return ShowRecordDialog(
            title,
            fields.Select(field => field.Key == "password"
                ? DialogField.Password(field.Key, field.Label)
                : DialogField.Text(field.Key, field.Label, field.Value)).ToArray());
    }

    private static IReadOnlyDictionary<string, string>? ShowRecordDialog(
        string title,
        params DialogField[] fields)
    {
        using var dialog = new Form
        {
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ClientSize = new Size(420, 92 + fields.Length * 54)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = fields.Length + 1,
            Padding = new Padding(16)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var inputs = new Dictionary<string, Control>();
        for (var index = 0; index < fields.Length; index++)
        {
            var field = fields[index];
            var input = BuildDialogInput(field);
            inputs[field.Key] = input;
            layout.RowStyles.Add(new RowStyle());
            layout.Controls.Add(new Label
            {
                AutoSize = true,
                Text = field.Label,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 10, 8)
            }, 0, index);
            layout.Controls.Add(input, 1, index);
        }

        var ok = new Button { AutoSize = true, Text = "Save", DialogResult = DialogResult.OK };
        var cancel = new Button { AutoSize = true, Text = "Cancel", DialogResult = DialogResult.Cancel };
        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill
        };
        actions.Controls.Add(cancel);
        actions.Controls.Add(ok);
        layout.Controls.Add(actions, 1, fields.Length);

        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;
        dialog.Controls.Add(layout);

        return dialog.ShowDialog() == DialogResult.OK
            ? inputs.ToDictionary(pair => pair.Key, pair => GetDialogValue(pair.Value))
            : null;
    }

    private static Control BuildDialogInput(DialogField field)
    {
        if (field.Options is not null)
        {
            var combo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = field.Options.ToArray(),
                DisplayMember = nameof(DialogOption.Label),
                ValueMember = nameof(DialogOption.Value)
            };
            combo.SelectedValue = field.Value;
            return combo;
        }

        return new TextBox
        {
            Dock = DockStyle.Fill,
            Text = field.Value,
            UseSystemPasswordChar = field.IsPassword
        };
    }

    private static string GetDialogValue(Control input)
    {
        return input switch
        {
            ComboBox combo => Convert.ToString(combo.SelectedValue) ?? string.Empty,
            TextBox textBox => textBox.Text.Trim(),
            _ => string.Empty
        };
    }

    private static int ParseRequiredInt(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!int.TryParse(values[key], out var result))
        {
            throw new InvalidOperationException($"{key} must be a number.");
        }

        return result;
    }

    private static int? ParseOptionalInt(IReadOnlyDictionary<string, string> values, string key)
    {
        if (string.IsNullOrWhiteSpace(values[key]))
        {
            return null;
        }

        return ParseRequiredInt(values, key);
    }

    private static DateTime? ParseOptionalDate(IReadOnlyDictionary<string, string> values, string key)
    {
        if (string.IsNullOrWhiteSpace(values[key]))
        {
            return null;
        }

        if (!DateTime.TryParse(values[key], out var result))
        {
            throw new InvalidOperationException($"{key} must be a valid date.");
        }

        return result.Date;
    }

    private static string FormatDate(DateTime? value) => value?.ToString("yyyy-MM-dd") ?? string.Empty;
}
