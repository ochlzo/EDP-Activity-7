namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    private (
        Control Panel,
        Label SiteNameValue,
        Label RiserIdValue,
        Label RiserNameValue,
        ComboBox RoomSort,
        DataGridView RoomsGrid,
        Label RoomStatus) BuildRiserDetailsPanel()
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
            Text = "Riser Details",
            Font = new Font(Font.FontFamily, 20, FontStyle.Bold)
        });
        headingText.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(560, 0),
            Text = "Review this riser and manage the rooms currently attached to it."
        });

        var addRoom = new Button
        {
            AutoSize = true,
            Text = "Add Room",
            Padding = new Padding(10, 6, 10, 6)
        };
        addRoom.Click += OnAddRoomClicked;

        var refresh = new Button
        {
            AutoSize = true,
            Text = "Refresh",
            Padding = new Padding(10, 6, 10, 6)
        };
        refresh.Click += OnRefreshRoomsClicked;

        var back = new Button
        {
            AutoSize = true,
            Text = "Back to Site Details",
            Padding = new Padding(10, 6, 10, 6)
        };
        back.Click += (_, _) => ShowSiteDetailsView();

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        actions.Controls.Add(addRoom);
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

        var siteNameValue = new Label { AutoSize = true, Text = "-" };
        var riserIdValue = new Label { AutoSize = true, Text = "-" };
        var riserNameValue = new Label { AutoSize = true, Text = "-" };

        var details = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 3,
            Margin = new Padding(0, 0, 0, 18)
        };
        details.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        details.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        details.Controls.Add(new Label { AutoSize = true, Text = "Site", Font = new Font(Font, FontStyle.Bold) }, 0, 0);
        details.Controls.Add(siteNameValue, 1, 0);
        details.Controls.Add(new Label { AutoSize = true, Text = "Riser ID", Font = new Font(Font, FontStyle.Bold) }, 0, 1);
        details.Controls.Add(riserIdValue, 1, 1);
        details.Controls.Add(new Label { AutoSize = true, Text = "Riser Name", Font = new Font(Font, FontStyle.Bold) }, 0, 2);
        details.Controls.Add(riserNameValue, 1, 2);

        var roomSortLabel = new Label
        {
            AutoSize = true,
            Text = "Sort by Room Name",
            Anchor = AnchorStyles.Left
        };

        var roomSort = new ComboBox
        {
            Dock = DockStyle.Left,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 170
        };
        roomSort.Items.AddRange(["Ascending", "Descending"]);
        roomSort.SelectedIndex = 0;
        roomSort.SelectedIndexChanged += OnRoomSortChanged;

        var roomSortRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 3,
            Margin = new Padding(0, 0, 0, 12)
        };
        roomSortRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        roomSortRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        roomSortRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        roomSortRow.Controls.Add(roomSortLabel, 0, 0);
        roomSortRow.Controls.Add(roomSort, 1, 0);

        var roomStatus = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(720, 0),
            Margin = new Padding(0, 0, 0, 12)
        };

        var roomsGrid = new DataGridView
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
            DataSource = _riserRoomsBindingSource
        };
        roomsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "RoomId",
            HeaderText = "Room ID",
            DataPropertyName = nameof(OwnedRoom.RoomId),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        });
        roomsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "RoomName",
            HeaderText = "Room Name",
            DataPropertyName = nameof(OwnedRoom.RoomName),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        roomsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Occupancy",
            HeaderText = "Occupancy",
            DataPropertyName = nameof(OwnedRoom.Occupancy),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        });
        roomsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Tenant",
            HeaderText = "Tenant",
            DataPropertyName = nameof(OwnedRoom.TenantDisplay),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        roomsGrid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "AddTenant",
            HeaderText = string.Empty,
            Text = "Add Tenant",
            UseColumnTextForButtonValue = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        });
        roomsGrid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "UpdateRoom",
            HeaderText = string.Empty,
            Text = "Update",
            UseColumnTextForButtonValue = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        });
        roomsGrid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "DeleteRoom",
            HeaderText = string.Empty,
            Text = "Delete",
            UseColumnTextForButtonValue = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        });
        roomsGrid.CellContentClick += OnRiserRoomsGridCellContentClick;
        roomsGrid.CellClick += OnRiserRoomsGridCellClick;
        roomsGrid.CellMouseMove += OnRiserRoomsGridCellMouseMove;
        roomsGrid.CellMouseLeave += OnRiserRoomsGridCellMouseLeave;

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Color.White,
            Padding = new Padding(26)
        };
        panel.RowStyles.Add(new RowStyle());
        panel.RowStyles.Add(new RowStyle());
        panel.RowStyles.Add(new RowStyle());
        panel.RowStyles.Add(new RowStyle());
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(header, 0, 0);
        panel.Controls.Add(details, 0, 1);
        panel.Controls.Add(roomSortRow, 0, 2);
        panel.Controls.Add(roomStatus, 0, 3);
        panel.Controls.Add(roomsGrid, 0, 4);

        return (panel, siteNameValue, riserIdValue, riserNameValue, roomSort, roomsGrid, roomStatus);
    }
}
