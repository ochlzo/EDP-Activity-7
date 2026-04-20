using MySqlConnector;

namespace edp_gui_app;

public sealed partial class SiteOwnerAuthService
{
    public async Task<IReadOnlyList<OwnedRoom>> LoadRoomsBySiteAsync(
        int siteId,
        int ownerId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT room.room_id, room.room_name, room.tenant_id IS NOT NULL AS is_occupied
            FROM room
            INNER JOIN riser ON riser.riser_id = room.riser_id
            INNER JOIN site ON site.site_id = riser.site_id
            WHERE site.site_id = @siteId AND site.owner_id = @ownerId
            ORDER BY room.room_name, room.room_id;
            """;
        command.Parameters.AddWithValue("@siteId", siteId);
        command.Parameters.AddWithValue("@ownerId", ownerId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rooms = new List<OwnedRoom>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rooms.Add(new OwnedRoom(
                reader.GetInt32("room_id"),
                reader.GetString("room_name"),
                reader.GetBoolean("is_occupied")));
        }

        return rooms;
    }

    public async Task<IReadOnlyList<OwnedRoom>> LoadRoomsByRiserAsync(
        int riserId,
        int siteId,
        int ownerId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT room.room_id, room.room_name, room.tenant_id IS NOT NULL AS is_occupied
            FROM room
            INNER JOIN riser ON riser.riser_id = room.riser_id
            INNER JOIN site ON site.site_id = riser.site_id
            WHERE riser.riser_id = @riserId AND site.site_id = @siteId AND site.owner_id = @ownerId
            ORDER BY room.room_name, room.room_id;
            """;
        command.Parameters.AddWithValue("@riserId", riserId);
        command.Parameters.AddWithValue("@siteId", siteId);
        command.Parameters.AddWithValue("@ownerId", ownerId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rooms = new List<OwnedRoom>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rooms.Add(new OwnedRoom(
                reader.GetInt32("room_id"),
                reader.GetString("room_name"),
                reader.GetBoolean("is_occupied")));
        }

        return rooms;
    }

    public async Task<OwnedRoom> CreateRoomAsync(
        int riserId,
        int siteId,
        int ownerId,
        string roomName,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO room (room_name, riser_id, tenant_id)
            SELECT @roomName, riser.riser_id, NULL
            FROM riser
            INNER JOIN site ON site.site_id = riser.site_id
            WHERE riser.riser_id = @riserId AND site.site_id = @siteId AND site.owner_id = @ownerId;
            """;
        command.Parameters.AddWithValue("@roomName", roomName);
        command.Parameters.AddWithValue("@riserId", riserId);
        command.Parameters.AddWithValue("@siteId", siteId);
        command.Parameters.AddWithValue("@ownerId", ownerId);

        var rowsInserted = await command.ExecuteNonQueryAsync(cancellationToken);
        if (rowsInserted == 0)
        {
            throw new InvalidOperationException("Room could not be created.");
        }

        return new OwnedRoom(Convert.ToInt32(command.LastInsertedId), roomName, false);
    }

    public async Task UpdateRoomAsync(
        int roomId,
        int siteId,
        int ownerId,
        string roomName,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE room
            INNER JOIN riser ON riser.riser_id = room.riser_id
            INNER JOIN site ON site.site_id = riser.site_id
            SET room.room_name = @roomName
            WHERE room.room_id = @roomId AND site.site_id = @siteId AND site.owner_id = @ownerId;
            """;
        command.Parameters.AddWithValue("@roomName", roomName);
        command.Parameters.AddWithValue("@roomId", roomId);
        command.Parameters.AddWithValue("@siteId", siteId);
        command.Parameters.AddWithValue("@ownerId", ownerId);

        var rowsUpdated = await command.ExecuteNonQueryAsync(cancellationToken);
        if (rowsUpdated == 0)
        {
            throw new InvalidOperationException("Room could not be updated.");
        }
    }

    public async Task DeleteRoomAsync(
        int roomId,
        int siteId,
        int ownerId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE room
            FROM room
            INNER JOIN riser ON riser.riser_id = room.riser_id
            INNER JOIN site ON site.site_id = riser.site_id
            WHERE room.room_id = @roomId AND site.site_id = @siteId AND site.owner_id = @ownerId;
            """;
        command.Parameters.AddWithValue("@roomId", roomId);
        command.Parameters.AddWithValue("@siteId", siteId);
        command.Parameters.AddWithValue("@ownerId", ownerId);

        var rowsDeleted = await command.ExecuteNonQueryAsync(cancellationToken);
        if (rowsDeleted == 0)
        {
            throw new InvalidOperationException("Room could not be deleted.");
        }
    }
}
