using MySqlConnector;

namespace edp_gui_admin;

public sealed partial class AdminTransactionService
{
    public async Task AssignTenantToRoomAsync(
        int roomId,
        int tenantId,
        string createdBy,
        string notes,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await UpdateRoomTenantAsync(connection, transaction, roomId, tenantId, cancellationToken);
        await InsertOccupancyTransactionAsync(
            connection,
            transaction,
            roomId,
            tenantId,
            "Assigned",
            notes,
            createdBy,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task VacateRoomAsync(
        int roomId,
        string createdBy,
        string notes,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var tenantId = await LoadRoomTenantIdAsync(connection, transaction, roomId, cancellationToken);
        await UpdateRoomTenantAsync(connection, transaction, roomId, null, cancellationToken);
        await InsertOccupancyTransactionAsync(
            connection,
            transaction,
            roomId,
            tenantId,
            "Vacated",
            notes,
            createdBy,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task TransferTenantAsync(
        int fromRoomId,
        int toRoomId,
        int tenantId,
        string createdBy,
        string notes,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await UpdateRoomTenantAsync(connection, transaction, fromRoomId, null, cancellationToken);
        await UpdateRoomTenantAsync(connection, transaction, toRoomId, tenantId, cancellationToken);
        await InsertOccupancyTransactionAsync(
            connection,
            transaction,
            toRoomId,
            tenantId,
            "Transferred",
            notes,
            createdBy,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminOccupancyTransaction>> LoadOccupancyTransactionsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT tx.occupancy_transaction_id, tx.room_id, room.room_name,
                   tx.tenant_id, tenant.tenant_name, tx.transaction_type,
                   tx.effective_at, tx.notes
            FROM room_occupancy_transaction tx
            INNER JOIN room ON room.room_id = tx.room_id
            LEFT JOIN tenant ON tenant.tenant_id = tx.tenant_id
            ORDER BY tx.effective_at DESC, tx.occupancy_transaction_id DESC;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var transactions = new List<AdminOccupancyTransaction>();
        while (await reader.ReadAsync(cancellationToken))
        {
            transactions.Add(new AdminOccupancyTransaction(
                reader.GetInt32("occupancy_transaction_id"),
                reader.GetInt32("room_id"),
                GetNullableString(reader, "room_name"),
                reader.IsDBNull(reader.GetOrdinal("tenant_id")) ? null : reader.GetInt32("tenant_id"),
                GetNullableString(reader, "tenant_name"),
                GetNullableString(reader, "transaction_type"),
                reader.GetDateTime("effective_at"),
                GetNullableString(reader, "notes")));
        }

        return transactions;
    }

    public async Task<IReadOnlyList<AdminOccupancyHistoryRow>> LoadOccupancyHistoryAsync(
        int? roomId = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT tx.occupancy_transaction_id, tx.room_id, room.room_name,
                   tx.tenant_id, tenant.tenant_name, tx.effective_at, tx.notes
            FROM room_occupancy_transaction tx
            INNER JOIN room ON room.room_id = tx.room_id
            LEFT JOIN tenant ON tenant.tenant_id = tx.tenant_id
            WHERE (@roomId IS NULL OR tx.room_id = @roomId)
            ORDER BY tx.effective_at DESC, tx.occupancy_transaction_id DESC;
            """;
        command.Parameters.AddWithValue("@roomId", roomId is null ? DBNull.Value : roomId.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var historyRows = new List<AdminOccupancyHistoryRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            historyRows.Add(new AdminOccupancyHistoryRow(
                reader.GetInt32("occupancy_transaction_id"),
                GetNullableString(reader, "tenant_name"),
                reader.IsDBNull(reader.GetOrdinal("tenant_id")) ? null : reader.GetInt32("tenant_id"),
                GetNullableString(reader, "room_name"),
                reader.GetInt32("room_id"),
                reader.GetDateTime("effective_at"),
                GetNullableString(reader, "notes")));
        }

        return historyRows;
    }

    private static async Task UpdateRoomTenantAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        int roomId,
        int? tenantId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE room SET tenant_id = @tenantId WHERE room_id = @roomId;";
        command.Parameters.AddWithValue("@tenantId", tenantId);
        command.Parameters.AddWithValue("@roomId", roomId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Room could not be updated.");
        }
    }

    private static async Task<int?> LoadRoomTenantIdAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        int roomId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT tenant_id FROM room WHERE room_id = @roomId;";
        command.Parameters.AddWithValue("@roomId", roomId);
        var tenantId = await command.ExecuteScalarAsync(cancellationToken);
        return tenantId is null || tenantId == DBNull.Value ? null : Convert.ToInt32(tenantId);
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
