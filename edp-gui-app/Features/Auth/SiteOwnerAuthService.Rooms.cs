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
        await EnsureTenantDetailsColumnsAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT room.room_id, room.room_name, room.tenant_id, tenant.tenant_name,
                   tenant.tenant_email,
                   room.tenant_id IS NOT NULL AS is_occupied
            FROM room
            INNER JOIN riser ON riser.riser_id = room.riser_id
            INNER JOIN site ON site.site_id = riser.site_id
            LEFT JOIN tenant ON tenant.tenant_id = room.tenant_id
            WHERE site.site_id = @siteId AND site.owner_id = @ownerId
            ORDER BY room.room_name, room.room_id;
            """;
        command.Parameters.AddWithValue("@siteId", siteId);
        command.Parameters.AddWithValue("@ownerId", ownerId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rooms = new List<OwnedRoom>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rooms.Add(ReadOwnedRoom(reader));
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
        await EnsureTenantDetailsColumnsAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT room.room_id, room.room_name, room.tenant_id, tenant.tenant_name,
                   tenant.tenant_email,
                   room.tenant_id IS NOT NULL AS is_occupied
            FROM room
            INNER JOIN riser ON riser.riser_id = room.riser_id
            INNER JOIN site ON site.site_id = riser.site_id
            LEFT JOIN tenant ON tenant.tenant_id = room.tenant_id
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
            rooms.Add(ReadOwnedRoom(reader));
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

    public async Task<OwnedRoom> CreateTenantInRoomAsync(
        int roomId,
        int siteId,
        int ownerId,
        string tenantName,
        string tenantEmail = "",
        string tenantAddress = "",
        string tenantContactNumber = "",
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureTenantDetailsColumnsAsync(connection, cancellationToken);
        await EnsureOccupancyTransactionSchemaAsync(connection, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            string roomName;
            await using (var roomCommand = connection.CreateCommand())
            {
                roomCommand.Transaction = transaction;
                roomCommand.CommandText = """
                    SELECT room.room_name, room.tenant_id
                    FROM room
                    INNER JOIN riser ON riser.riser_id = room.riser_id
                    INNER JOIN site ON site.site_id = riser.site_id
                    WHERE room.room_id = @roomId AND site.site_id = @siteId AND site.owner_id = @ownerId
                    FOR UPDATE;
                    """;
                roomCommand.Parameters.AddWithValue("@roomId", roomId);
                roomCommand.Parameters.AddWithValue("@siteId", siteId);
                roomCommand.Parameters.AddWithValue("@ownerId", ownerId);

                await using var reader = await roomCommand.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    throw new InvalidOperationException("Room could not be found.");
                }

                roomName = reader.GetString("room_name");
                if (!reader.IsDBNull(reader.GetOrdinal("tenant_id")))
                {
                    throw new InvalidOperationException("Room is already occupied.");
                }
            }

            int tenantId;
            await using (var tenantCommand = connection.CreateCommand())
            {
                tenantCommand.Transaction = transaction;
                tenantCommand.CommandText = """
                    INSERT INTO tenant
                        (tenant_name, tenant_email, tenant_address, tenant_contact_number)
                    VALUES
                        (@tenantName, @tenantEmail, @tenantAddress, @tenantContactNumber);
                    """;
                tenantCommand.Parameters.AddWithValue("@tenantName", tenantName);
                tenantCommand.Parameters.AddWithValue("@tenantEmail", tenantEmail);
                tenantCommand.Parameters.AddWithValue("@tenantAddress", tenantAddress);
                tenantCommand.Parameters.AddWithValue("@tenantContactNumber", tenantContactNumber);

                await tenantCommand.ExecuteNonQueryAsync(cancellationToken);
                tenantId = Convert.ToInt32(tenantCommand.LastInsertedId);
            }

            await using (var updateCommand = connection.CreateCommand())
            {
                updateCommand.Transaction = transaction;
                updateCommand.CommandText = "UPDATE room SET tenant_id = @tenantId WHERE room_id = @roomId;";
                updateCommand.Parameters.AddWithValue("@tenantId", tenantId);
                updateCommand.Parameters.AddWithValue("@roomId", roomId);

                await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await InsertOccupancyTransactionAsync(
                connection,
                transaction,
                roomId,
                tenantId,
                "Assigned",
                $"Assigned tenant {tenantName} to room {roomName}.",
                $"owner:{ownerId}",
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new OwnedRoom(roomId, roomName, true, tenantId, tenantName, tenantEmail);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static OwnedRoom ReadOwnedRoom(MySqlDataReader reader)
    {
        var tenantIdOrdinal = reader.GetOrdinal("tenant_id");
        var tenantNameOrdinal = reader.GetOrdinal("tenant_name");
        var tenantEmailOrdinal = reader.GetOrdinal("tenant_email");

        return new OwnedRoom(
            reader.GetInt32("room_id"),
            reader.GetString("room_name"),
            reader.GetBoolean("is_occupied"),
            reader.IsDBNull(tenantIdOrdinal) ? null : reader.GetInt32(tenantIdOrdinal),
            reader.IsDBNull(tenantNameOrdinal) ? null : reader.GetString(tenantNameOrdinal),
            reader.IsDBNull(tenantEmailOrdinal) ? null : reader.GetString(tenantEmailOrdinal));
    }
}
