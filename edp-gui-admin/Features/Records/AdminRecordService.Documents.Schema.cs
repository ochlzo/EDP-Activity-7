using MySqlConnector;

namespace edp_gui_admin;

public sealed partial class AdminRecordService
{
    private static async Task EnsureDocumentComplianceColumnsAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await AddDocumentColumnAsync(connection, "doc_type", "VARCHAR(80) NOT NULL DEFAULT 'General'", cancellationToken);
        await AddDocumentColumnAsync(connection, "doc_status", "VARCHAR(40) NOT NULL DEFAULT 'Active'", cancellationToken);
        await AddDocumentColumnAsync(connection, "issued_at", "DATE NULL", cancellationToken);
        await AddDocumentColumnAsync(connection, "submitted_at", "DATE NULL", cancellationToken);
        await DropDocumentColumnAsync(connection, "expires_at", cancellationToken);
        await AddDocumentColumnAsync(connection, "notes", "TEXT NULL", cancellationToken);
        await AddDocumentColumnAsync(connection, "created_at", "DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP", cancellationToken);
        await AddDocumentColumnAsync(connection, "updated_at", "DATETIME NULL", cancellationToken);
    }

    private static async Task AddDocumentColumnAsync(
        MySqlConnection connection,
        string columnName,
        string definition,
        CancellationToken cancellationToken)
    {
        if (await DocumentColumnExistsAsync(connection, columnName, cancellationToken))
        {
            return;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER TABLE document ADD COLUMN {columnName} {definition};";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task DropDocumentColumnAsync(
        MySqlConnection connection,
        string columnName,
        CancellationToken cancellationToken)
    {
        if (!await DocumentColumnExistsAsync(connection, columnName, cancellationToken))
        {
            return;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER TABLE document DROP COLUMN {columnName};";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> DocumentColumnExistsAsync(
        MySqlConnection connection,
        string columnName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
                AND table_name = 'document'
                AND column_name = @columnName;
            """;
        command.Parameters.AddWithValue("@columnName", columnName);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }
}
