using MySqlConnector;

namespace edp_gui_admin;

public sealed partial class AdminRecordService
{
    public async Task<IReadOnlyList<AdminRoom>> LoadRoomsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT room.room_id, room.room_name, room.riser_id, riser.riser_name,
                   room.tenant_id, tenant.tenant_name
            FROM room
            INNER JOIN riser ON riser.riser_id = room.riser_id
            LEFT JOIN tenant ON tenant.tenant_id = room.tenant_id
            ORDER BY room.room_name, room.room_id;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rooms = new List<AdminRoom>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rooms.Add(ReadRoom(reader));
        }

        return rooms;
    }

    public async Task<AdminRoom> CreateRoomAsync(
        string roomName,
        int riserId,
        int? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO room (room_name, riser_id, tenant_id)
            VALUES (@roomName, @riserId, @tenantId);
            """;
        command.Parameters.AddWithValue("@roomName", roomName);
        command.Parameters.AddWithValue("@riserId", riserId);
        command.Parameters.AddWithValue("@tenantId", tenantId);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var room = new AdminRoom(
            Convert.ToInt32(command.LastInsertedId),
            roomName,
            riserId,
            await LoadRiserNameAsync(connection, riserId, cancellationToken),
            tenantId,
            await LoadTenantNameAsync(connection, tenantId, cancellationToken));
        await LogAdminActivityAsync(
            "Created",
            "Room",
            room.RoomId,
            $"Created room {room.RoomName}.",
            cancellationToken);
        return room;
    }

    public async Task UpdateRoomAsync(
        int roomId,
        string roomName,
        int riserId,
        int? tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE room
            SET room_name = @roomName, riser_id = @riserId, tenant_id = @tenantId
            WHERE room_id = @roomId;
            """;
        command.Parameters.AddWithValue("@roomName", roomName);
        command.Parameters.AddWithValue("@riserId", riserId);
        command.Parameters.AddWithValue("@tenantId", tenantId);
        command.Parameters.AddWithValue("@roomId", roomId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Room could not be updated.");
        }

        await LogAdminActivityAsync(
            "Updated",
            "Room",
            roomId,
            $"Updated room {roomName}.",
            cancellationToken);
    }

    public async Task DeleteRoomAsync(int roomId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM room WHERE room_id = @roomId;";
        command.Parameters.AddWithValue("@roomId", roomId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Room could not be deleted.");
        }

        await LogAdminActivityAsync(
            "Deleted",
            "Room",
            roomId,
            $"Deleted room {roomId}.",
            cancellationToken);
    }

    private static AdminRoom ReadRoom(MySqlDataReader reader)
    {
        var tenantIdOrdinal = reader.GetOrdinal("tenant_id");
        var tenantId = reader.IsDBNull(tenantIdOrdinal) ? null : (int?)reader.GetInt32(tenantIdOrdinal);

        return new AdminRoom(
            reader.GetInt32("room_id"),
            GetNullableString(reader, "room_name"),
            reader.GetInt32("riser_id"),
            GetNullableString(reader, "riser_name"),
            tenantId,
            GetNullableString(reader, "tenant_name"));
    }

    private static async Task<string> LoadRiserNameAsync(
        MySqlConnection connection,
        int riserId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT riser_name FROM riser WHERE riser_id = @riserId;";
        command.Parameters.AddWithValue("@riserId", riserId);
        return Convert.ToString(await command.ExecuteScalarAsync(cancellationToken)) ?? string.Empty;
    }

    private static async Task<string> LoadTenantNameAsync(
        MySqlConnection connection,
        int? tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantId is null)
        {
            return string.Empty;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT tenant_name FROM tenant WHERE tenant_id = @tenantId;";
        command.Parameters.AddWithValue("@tenantId", tenantId.Value);
        return Convert.ToString(await command.ExecuteScalarAsync(cancellationToken)) ?? string.Empty;
    }
}
