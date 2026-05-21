namespace edp_gui_admin;

public sealed partial class AdminTransactionService
{
    public async Task LogActivityAsync(
        string actorType,
        string actorName,
        string action,
        string entityType,
        int entityId,
        string description,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO activity_log
                (actor_type, actor_name, action, entity_type, entity_id, description)
            VALUES
                (@actorType, @actorName, @action, @entityType, @entityId, @description);
            """;
        command.Parameters.AddWithValue("@actorType", actorType);
        command.Parameters.AddWithValue("@actorName", actorName);
        command.Parameters.AddWithValue("@action", action);
        command.Parameters.AddWithValue("@entityType", entityType);
        command.Parameters.AddWithValue("@entityId", entityId);
        command.Parameters.AddWithValue("@description", description);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminActivityLog>> LoadActivityLogsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT activity_id, actor_type, actor_name, action,
                   entity_type, entity_id, description, created_at
            FROM activity_log
            ORDER BY created_at DESC, activity_id DESC;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var logs = new List<AdminActivityLog>();
        while (await reader.ReadAsync(cancellationToken))
        {
            logs.Add(new AdminActivityLog(
                reader.GetInt32("activity_id"),
                GetNullableString(reader, "actor_type"),
                GetNullableString(reader, "actor_name"),
                GetNullableString(reader, "action"),
                GetNullableString(reader, "entity_type"),
                reader.GetInt32("entity_id"),
                GetNullableString(reader, "description"),
                reader.GetDateTime("created_at")));
        }

        return logs;
    }
}
