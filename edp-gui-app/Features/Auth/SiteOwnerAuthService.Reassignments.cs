using MySqlConnector;

namespace edp_gui_app;

public sealed partial class SiteOwnerAuthService
{
    public async Task<OwnedRoom> ReplaceTenantInRoomAsync(
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
            string previousTenantName = string.Empty;

            await using (var roomCommand = connection.CreateCommand())
            {
                roomCommand.Transaction = transaction;
                roomCommand.CommandText = """
                    SELECT room.room_name, tenant.tenant_name
                    FROM room
                    INNER JOIN riser ON riser.riser_id = room.riser_id
                    INNER JOIN site ON site.site_id = riser.site_id
                    LEFT JOIN tenant ON tenant.tenant_id = room.tenant_id
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
                previousTenantName = GetNullableString(reader, "tenant_name");
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

            await using (var clearCommand = connection.CreateCommand())
            {
                clearCommand.Transaction = transaction;
                clearCommand.CommandText = "UPDATE room SET tenant_id = NULL WHERE room_id = @roomId;";
                clearCommand.Parameters.AddWithValue("@roomId", roomId);

                await clearCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var updateCommand = connection.CreateCommand())
            {
                updateCommand.Transaction = transaction;
                updateCommand.CommandText = "UPDATE room SET tenant_id = @tenantId WHERE room_id = @roomId;";
                updateCommand.Parameters.AddWithValue("@tenantId", tenantId);
                updateCommand.Parameters.AddWithValue("@roomId", roomId);

                await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            var note = string.IsNullOrWhiteSpace(previousTenantName)
                ? $"Replaced room {roomName} tenant with {tenantName}."
                : $"Replaced tenant {previousTenantName} with {tenantName} in room {roomName}.";

            await InsertOccupancyTransactionAsync(
                connection,
                transaction,
                roomId,
                tenantId,
                "Replaced",
                note,
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
}
