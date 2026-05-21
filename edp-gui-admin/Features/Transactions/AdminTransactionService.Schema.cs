using MySqlConnector;

namespace edp_gui_admin;

public sealed partial class AdminTransactionService
{
    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await AddDocumentColumnAsync(connection, "doc_type", "VARCHAR(80) NOT NULL DEFAULT 'General'", cancellationToken);
        await AddDocumentColumnAsync(connection, "doc_status", "VARCHAR(40) NOT NULL DEFAULT 'Active'", cancellationToken);
        await AddDocumentColumnAsync(connection, "issued_at", "DATE NULL", cancellationToken);
        await AddDocumentColumnAsync(connection, "submitted_at", "DATE NULL", cancellationToken);
        await DropDocumentColumnAsync(connection, "expires_at", cancellationToken);
        await AddDocumentColumnAsync(connection, "notes", "TEXT NULL", cancellationToken);
        await AddDocumentColumnAsync(connection, "created_at", "DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP", cancellationToken);
        await AddDocumentColumnAsync(connection, "updated_at", "DATETIME NULL", cancellationToken);

        await ExecuteAsync(connection, """
            CREATE TABLE IF NOT EXISTS room_occupancy_transaction (
                occupancy_transaction_id INT AUTO_INCREMENT PRIMARY KEY,
                room_id INT NOT NULL,
                tenant_id INT NULL,
                transaction_type VARCHAR(40) NOT NULL,
                effective_at DATETIME NOT NULL,
                notes TEXT NULL,
                created_by VARCHAR(255) NOT NULL,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (room_id) REFERENCES room(room_id) ON DELETE CASCADE,
                FOREIGN KEY (tenant_id) REFERENCES tenant(tenant_id) ON DELETE SET NULL
            );
            """, cancellationToken);

        await ExecuteAsync(connection, """
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
            """, cancellationToken);

        await AddMaintenanceColumnAsync(connection, "notes", "TEXT NULL", cancellationToken);

        await ExecuteAsync(connection, """
            CREATE TABLE IF NOT EXISTS maintenance_ticket_status_history (
                history_id INT AUTO_INCREMENT PRIMARY KEY,
                ticket_id INT NOT NULL,
                old_status VARCHAR(40) NULL,
                new_status VARCHAR(40) NOT NULL,
                changed_by VARCHAR(255) NOT NULL,
                changed_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                notes TEXT NULL,
                FOREIGN KEY (ticket_id) REFERENCES maintenance_ticket(ticket_id) ON DELETE CASCADE
            );
            """, cancellationToken);

        await ExecuteAsync(connection, """
            CREATE TABLE IF NOT EXISTS activity_log (
                activity_id INT AUTO_INCREMENT PRIMARY KEY,
                actor_type VARCHAR(40) NOT NULL,
                actor_name VARCHAR(255) NOT NULL,
                action VARCHAR(80) NOT NULL,
                entity_type VARCHAR(80) NOT NULL,
                entity_id INT NOT NULL,
                owner_id INT NULL,
                site_id INT NULL,
                riser_id INT NULL,
                room_id INT NULL,
                tenant_id INT NULL,
                document_id INT NULL,
                description TEXT NOT NULL,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """, cancellationToken);
    }

    private static async Task AddDocumentColumnAsync(
        MySqlConnection connection,
        string columnName,
        string definition,
        CancellationToken cancellationToken)
    {
        if (await ColumnExistsAsync(connection, "document", columnName, cancellationToken))
        {
            return;
        }

        await ExecuteAsync(connection, $"ALTER TABLE document ADD COLUMN {columnName} {definition};", cancellationToken);
    }

    private static async Task DropDocumentColumnAsync(
        MySqlConnection connection,
        string columnName,
        CancellationToken cancellationToken)
    {
        if (await ColumnExistsAsync(connection, "document", columnName, cancellationToken))
        {
            await ExecuteAsync(connection, $"ALTER TABLE document DROP COLUMN {columnName};", cancellationToken);
        }
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

        await ExecuteAsync(
            connection,
            $"ALTER TABLE maintenance_ticket ADD COLUMN {columnName} {definition};",
            cancellationToken);
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

    private static async Task ExecuteAsync(
        MySqlConnection connection,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
