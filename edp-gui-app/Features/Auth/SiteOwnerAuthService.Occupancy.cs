using MySqlConnector;

namespace edp_gui_app;

public sealed partial class SiteOwnerAuthService
{
    private static async Task EnsureOccupancyTransactionSchemaAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS room_occupancy_transaction (
                occupancy_transaction_id INT AUTO_INCREMENT PRIMARY KEY,
                room_id INT NOT NULL,
                tenant_id INT NULL,
                transaction_type VARCHAR(40) NOT NULL,
                effective_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                notes TEXT NULL,
                created_by VARCHAR(255) NOT NULL,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (room_id) REFERENCES room(room_id) ON DELETE CASCADE,
                FOREIGN KEY (tenant_id) REFERENCES tenant(tenant_id) ON DELETE SET NULL
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertOccupancyTransactionAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        int roomId,
        int? tenantId,
        string transactionType,
        string notes,
        string createdBy,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO room_occupancy_transaction
                (room_id, tenant_id, transaction_type, effective_at, notes, created_by)
            VALUES
                (@roomId, @tenantId, @transactionType, @effectiveAt, @notes, @createdBy);
            """;
        command.Parameters.AddWithValue("@roomId", roomId);
        command.Parameters.AddWithValue("@tenantId", tenantId);
        command.Parameters.AddWithValue("@transactionType", transactionType);
        command.Parameters.AddWithValue("@effectiveAt", DateTime.Now);
        command.Parameters.AddWithValue("@notes", notes);
        command.Parameters.AddWithValue("@createdBy", createdBy);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
