namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private (Control Panel, Label SiteIdValue, Label SiteNameValue, DataGridView RisersGrid, Label RiserStatus)
        BuildSiteDetailsPanel()
    {
        var headingText = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0)
        };
        headingText.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Site Details",
            Font = new Font(Font.FontFamily, 20, FontStyle.Bold)
        });
        headingText.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(560, 0),
            Text = "Review this site and manage the risers currently attached to it."
        });

        var addRiser = new Button
        {
            AutoSize = true,
            Text = "Add Riser",
            Padding = new Padding(10, 6, 10, 6)
        };
        addRiser.Click += OnAddRiserClicked;

        var refresh = new Button
        {
            AutoSize = true,
            Text = "Refresh",
            Padding = new Padding(10, 6, 10, 6)
        };
        refresh.Click += OnRefreshRisersClicked;

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
            WrapContents = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        actions.Controls.Add(addRiser);
        actions.Controls.Add(refresh);
        actions.Controls.Add(back);

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            Margin = new Padding(0, 0, 0, 18)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(headingText, 0, 0);
        header.Controls.Add(actions, 1, 0);

        var siteIdValue = new Label { AutoSize = true, Text = "-" };
        var siteNameValue = new Label { AutoSize = true, Text = "-" };

        var details = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0, 0, 0, 18)
        };
        details.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        details.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        details.Controls.Add(new Label { AutoSize = true, Text = "Site ID", Font = new Font(Font, FontStyle.Bold) }, 0, 0);
        details.Controls.Add(siteIdValue, 1, 0);
        details.Controls.Add(new Label { AutoSize = true, Text = "Site Name", Font = new Font(Font, FontStyle.Bold) }, 0, 1);
        details.Controls.Add(siteNameValue, 1, 1);

        var riserStatus = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(720, 0),
            Margin = new Padding(0, 0, 0, 12)
        };

        var risersGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            DataSource = _siteRisersBindingSource
        };
        risersGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "RiserId",
            HeaderText = "Riser ID",
            DataPropertyName = nameof(OwnedRiser.RiserId),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        });
        risersGrid.Columns.Add(new DataGridViewLinkColumn
        {
            Name = "RiserName",
            HeaderText = "Riser Name",
            DataPropertyName = nameof(OwnedRiser.RiserName),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            LinkBehavior = LinkBehavior.HoverUnderline,
            TrackVisitedState = false
        });
        risersGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "RoomCount",
            HeaderText = "How Many Rooms",
            DataPropertyName = nameof(OwnedRiser.RoomCount),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        });
        risersGrid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "EditRiser",
            HeaderText = string.Empty,
            Text = "Edit",
            UseColumnTextForButtonValue = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        });
        risersGrid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "DeleteRiser",
            HeaderText = string.Empty,
            Text = "Delete",
            UseColumnTextForButtonValue = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        });
        risersGrid.CellContentClick += OnSiteRisersGridCellContentClick;

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.White,
            Padding = new Padding(26)
        };
        panel.RowStyles.Add(new RowStyle());
        panel.RowStyles.Add(new RowStyle());
        panel.RowStyles.Add(new RowStyle());
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(header, 0, 0);
        panel.Controls.Add(details, 0, 1);
        panel.Controls.Add(riserStatus, 0, 2);
        panel.Controls.Add(risersGrid, 0, 3);

        return (panel, siteIdValue, siteNameValue, risersGrid, riserStatus);
    }
}
