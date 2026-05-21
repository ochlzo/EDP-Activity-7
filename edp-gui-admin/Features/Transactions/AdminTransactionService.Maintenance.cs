namespace edp_gui_admin;

public sealed partial class AdminTransactionService
{
    public async Task UpdateMaintenanceTicketStatusAsync(
        int ticketId,
        string newStatus,
        string changedBy,
        string notes,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var oldStatus = await LoadMaintenanceStatusAsync(connection, transaction, ticketId, cancellationToken);
        await using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE maintenance_ticket
            SET status = @status,
                resolved_at = CASE WHEN @status = 'Resolved' THEN UTC_TIMESTAMP() ELSE resolved_at END
            WHERE ticket_id = @ticketId;
            """;
        update.Parameters.AddWithValue("@status", newStatus);
        update.Parameters.AddWithValue("@ticketId", ticketId);
        if (await update.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Maintenance ticket could not be updated.");
        }

        await using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO maintenance_ticket_status_history
                (ticket_id, old_status, new_status, changed_by, notes)
            VALUES
                (@ticketId, @oldStatus, @newStatus, @changedBy, @notes);
            """;
        insert.Parameters.AddWithValue("@ticketId", ticketId);
        insert.Parameters.AddWithValue("@oldStatus", oldStatus);
        insert.Parameters.AddWithValue("@newStatus", newStatus);
        insert.Parameters.AddWithValue("@changedBy", changedBy);
        insert.Parameters.AddWithValue("@notes", notes);
        await insert.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        await LogActivityAsync(
            "Admin",
            changedBy,
            "Updated",
            "MaintenanceTicket",
            ticketId,
            $"Changed maintenance ticket {ticketId} from {oldStatus} to {newStatus}.",
            cancellationToken);
    }

    public async Task UpdateMaintenanceTicketAsync(
        int ticketId,
        string title,
        string priority,
        string status,
        string notes,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var oldStatus = await LoadMaintenanceStatusAsync(connection, transaction, ticketId, cancellationToken);
        await using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE maintenance_ticket
            SET title = @title,
                priority = @priority,
                status = @status,
                notes = @notes,
                resolved_at = CASE WHEN @status = 'Resolved' THEN @resolvedAt ELSE resolved_at END
            WHERE ticket_id = @ticketId;
            """;
        update.Parameters.AddWithValue("@title", title);
        update.Parameters.AddWithValue("@priority", priority);
        update.Parameters.AddWithValue("@status", status);
        update.Parameters.AddWithValue("@notes", notes);
        update.Parameters.AddWithValue("@resolvedAt", DateTime.Now);
        update.Parameters.AddWithValue("@ticketId", ticketId);
        if (await update.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Maintenance ticket could not be updated.");
        }

        await using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO maintenance_ticket_status_history
                (ticket_id, old_status, new_status, changed_by, notes)
            VALUES
                (@ticketId, @oldStatus, @newStatus, @changedBy, @notes);
            """;
        insert.Parameters.AddWithValue("@ticketId", ticketId);
        insert.Parameters.AddWithValue("@oldStatus", oldStatus);
        insert.Parameters.AddWithValue("@newStatus", status);
        insert.Parameters.AddWithValue("@changedBy", changedBy);
        insert.Parameters.AddWithValue("@notes", notes);
        await insert.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        await LogActivityAsync(
            "Admin",
            changedBy,
            "Updated",
            "MaintenanceTicket",
            ticketId,
            $"Updated maintenance ticket {ticketId}.",
            cancellationToken);
    }

    public async Task ResolveMaintenanceTicketAsync(
        int ticketId,
        string notes,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var oldStatus = await LoadMaintenanceStatusAsync(connection, transaction, ticketId, cancellationToken);
        await using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE maintenance_ticket
            SET status = 'Resolved',
                resolved_at = @resolvedAt,
                notes = @notes
            WHERE ticket_id = @ticketId;
            """;
        update.Parameters.AddWithValue("@resolvedAt", DateTime.Now);
        update.Parameters.AddWithValue("@notes", notes);
        update.Parameters.AddWithValue("@ticketId", ticketId);
        if (await update.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Maintenance ticket could not be updated.");
        }

        await using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO maintenance_ticket_status_history
                (ticket_id, old_status, new_status, changed_by, notes)
            VALUES
                (@ticketId, @oldStatus, 'Resolved', @changedBy, @notes);
            """;
        insert.Parameters.AddWithValue("@ticketId", ticketId);
        insert.Parameters.AddWithValue("@oldStatus", oldStatus);
        insert.Parameters.AddWithValue("@changedBy", changedBy);
        insert.Parameters.AddWithValue("@notes", notes);
        await insert.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        await LogActivityAsync(
            "Admin",
            changedBy,
            "Resolved",
            "MaintenanceTicket",
            ticketId,
            $"Resolved maintenance ticket {ticketId}.",
            cancellationToken);
    }

    public async Task UpdateMaintenanceTicketNotesAsync(
        int ticketId,
        string notes,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE maintenance_ticket
            SET notes = @notes
            WHERE ticket_id = @ticketId;
            """;
        command.Parameters.AddWithValue("@notes", notes);
        command.Parameters.AddWithValue("@ticketId", ticketId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Maintenance ticket could not be updated.");
        }

        await LogActivityAsync(
            "Admin",
            changedBy,
            "Updated",
            "MaintenanceTicket",
            ticketId,
            $"Updated maintenance ticket {ticketId} notes.",
            cancellationToken);
    }

    public async Task DeleteMaintenanceTicketAsync(
        int ticketId,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM maintenance_ticket WHERE ticket_id = @ticketId;";
        command.Parameters.AddWithValue("@ticketId", ticketId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Maintenance ticket could not be deleted.");
        }

        await LogActivityAsync(
            "Admin",
            changedBy,
            "Deleted",
            "MaintenanceTicket",
            ticketId,
            $"Deleted maintenance ticket {ticketId}.",
            cancellationToken);
    }

    public async Task<IReadOnlyList<AdminMaintenanceHistory>> LoadMaintenanceHistoryAsync(
        int ticketId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT history_id, ticket_id, old_status, new_status, changed_by, changed_at, notes
            FROM maintenance_ticket_status_history
            WHERE ticket_id = @ticketId
            ORDER BY changed_at, history_id;
            """;
        command.Parameters.AddWithValue("@ticketId", ticketId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var history = new List<AdminMaintenanceHistory>();
        while (await reader.ReadAsync(cancellationToken))
        {
            history.Add(new AdminMaintenanceHistory(
                reader.GetInt32("history_id"),
                reader.GetInt32("ticket_id"),
                GetNullableString(reader, "old_status"),
                GetNullableString(reader, "new_status"),
                GetNullableString(reader, "changed_by"),
                reader.GetDateTime("changed_at"),
                GetNullableString(reader, "notes")));
        }

        return history;
    }

    private static async Task<string> LoadMaintenanceStatusAsync(
        MySqlConnector.MySqlConnection connection,
        MySqlConnector.MySqlTransaction transaction,
        int ticketId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT status FROM maintenance_ticket WHERE ticket_id = @ticketId;";
        command.Parameters.AddWithValue("@ticketId", ticketId);
        return Convert.ToString(await command.ExecuteScalarAsync(cancellationToken)) ??
            throw new InvalidOperationException("Maintenance ticket could not be found.");
    }
}
