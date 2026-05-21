using MySqlConnector;

namespace edp_gui_app;

public sealed class OwnerMaintenanceService
{
    private readonly string _connectionString;

    public OwnerMaintenanceService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<OwnerMaintenanceRequest> CreateMaintenanceTicketAsync(
        int ownerId,
        int siteId,
        int? riserId,
        int? roomId,
        string title,
        string description,
        string priority,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);

        if (!await OwnerHasSiteAsync(connection, ownerId, siteId, cancellationToken))
        {
            throw new InvalidOperationException("Maintenance ticket site is not owned by this owner.");
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO maintenance_ticket
                (site_id, riser_id, room_id, requested_by_owner_id, title, description, priority, status)
            VALUES
                (@siteId, @riserId, @roomId, @ownerId, @title, @description, @priority, 'Open');
            """;
        command.Parameters.AddWithValue("@siteId", siteId);
        command.Parameters.AddWithValue("@riserId", riserId);
        command.Parameters.AddWithValue("@roomId", roomId);
        command.Parameters.AddWithValue("@ownerId", ownerId);
        command.Parameters.AddWithValue("@title", title);
        command.Parameters.AddWithValue("@description", description);
        command.Parameters.AddWithValue("@priority", priority);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return (await LoadTicketsByOwnerAsync(ownerId, cancellationToken))
            .Single(ticket => ticket.TicketId == Convert.ToInt32(command.LastInsertedId));
    }

    public async Task<IReadOnlyList<OwnerMaintenanceRequest>> LoadTicketsByOwnerAsync(
        int ownerId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ticket.ticket_id, site.site_name, ticket.title, ticket.description,
                   ticket.priority, ticket.status, ticket.requested_at, ticket.resolved_at,
                   ticket.notes
            FROM maintenance_ticket ticket
            INNER JOIN site ON site.site_id = ticket.site_id
            WHERE ticket.requested_by_owner_id = @ownerId
            ORDER BY ticket.requested_at DESC, ticket.ticket_id DESC;
            """;
        command.Parameters.AddWithValue("@ownerId", ownerId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var tickets = new List<OwnerMaintenanceRequest>();
        while (await reader.ReadAsync(cancellationToken))
        {
            tickets.Add(new OwnerMaintenanceRequest(
                reader.GetInt32("ticket_id"),
                GetNullableString(reader, "site_name"),
                GetNullableString(reader, "title"),
                GetNullableString(reader, "description"),
                GetNullableString(reader, "priority"),
                GetNullableString(reader, "status"),
                reader.GetDateTime("requested_at"),
                reader.IsDBNull(reader.GetOrdinal("resolved_at")) ? null : reader.GetDateTime("resolved_at"),
                GetNullableString(reader, "notes")));
        }

        return tickets;
    }

    private static async Task EnsureSchemaAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS maintenance_ticket (
                ticket_id INT AUTO_INCREMENT PRIMARY KEY,
                site_id INT NOT NULL,
                riser_id INT NULL,
                room_id INT NULL,
                requested_by_owner_id INT NOT NULL,
                title VARCHAR(255) NOT NULL,
                description TEXT NOT NULL,
                priority VARCHAR(40) NOT NULL DEFAULT 'Normal',
                status VARCHAR(40) NOT NULL DEFAULT 'Open',
                requested_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                resolved_at DATETIME NULL,
                FOREIGN KEY (site_id) REFERENCES site(site_id) ON DELETE CASCADE,
                FOREIGN KEY (riser_id) REFERENCES riser(riser_id) ON DELETE SET NULL,
                FOREIGN KEY (room_id) REFERENCES room(room_id) ON DELETE SET NULL,
                FOREIGN KEY (requested_by_owner_id) REFERENCES site_owner(owner_id) ON DELETE CASCADE
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);

        await AddMaintenanceColumnAsync(connection, "notes", "TEXT NULL", cancellationToken);
    }

    private static async Task<bool> OwnerHasSiteAsync(
        MySqlConnection connection,
        int ownerId,
        int siteId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM site WHERE owner_id = @ownerId AND site_id = @siteId LIMIT 1;";
        command.Parameters.AddWithValue("@ownerId", ownerId);
        command.Parameters.AddWithValue("@siteId", siteId);
        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }

    private static async Task AddMaintenanceColumnAsync(
        MySqlConnection connection,
        string columnName,
        string definition,
        CancellationToken cancellationToken)
    {
        if (await ColumnExistsAsync(connection, "maintenance_ticket", columnName, cancellationToken))
        {
            return;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER TABLE maintenance_ticket ADD COLUMN {columnName} {definition};";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> ColumnExistsAsync(
        MySqlConnection connection,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
                AND table_name = @tableName
                AND column_name = @columnName;
            """;
        command.Parameters.AddWithValue("@tableName", tableName);
        command.Parameters.AddWithValue("@columnName", columnName);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    private static string GetNullableString(MySqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
    }
}
