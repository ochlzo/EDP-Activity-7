using MySqlConnector;

namespace edp_gui_admin;

public sealed partial class AdminRecordService
{
    private static async Task EnsureTenantDetailsColumnsAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await AddTenantColumnAsync(connection, "tenant_email", "VARCHAR(255) NOT NULL DEFAULT ''", cancellationToken);
        await AddTenantColumnAsync(connection, "tenant_address", "VARCHAR(255) NOT NULL DEFAULT ''", cancellationToken);
        await AddTenantColumnAsync(connection, "tenant_contact_number", "VARCHAR(80) NOT NULL DEFAULT ''", cancellationToken);
    }

    private static async Task AddTenantColumnAsync(
        MySqlConnection connection,
        string columnName,
        string definition,
        CancellationToken cancellationToken)
    {
        if (await TenantColumnExistsAsync(connection, columnName, cancellationToken))
        {
            return;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER TABLE tenant ADD COLUMN {columnName} {definition};";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> TenantColumnExistsAsync(
        MySqlConnection connection,
        string columnName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
                AND table_name = 'tenant'
                AND column_name = @columnName;
            """;
        command.Parameters.AddWithValue("@columnName", columnName);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }
}
