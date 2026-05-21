using MySqlConnector;

namespace edp_gui_admin;

public sealed partial class AdminRecordService
{
    public async Task<IReadOnlyList<AdminOwner>> LoadOwnersAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureOwnerStatusColumnAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT owner_id, owner_name, owner_email, is_active
            FROM site_owner
            ORDER BY owner_name, owner_id;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var owners = new List<AdminOwner>();
        while (await reader.ReadAsync(cancellationToken))
        {
            owners.Add(ReadOwner(reader));
        }

        return owners;
    }

    public async Task<AdminOwner> CreateOwnerAsync(
        string ownerName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureOwnerStatusColumnAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO site_owner (owner_name, owner_email, password, is_active)
            VALUES (@name, @email, @password, 1);
            """;
        command.Parameters.AddWithValue("@name", ownerName);
        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@password", password);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var ownerId = Convert.ToInt32(command.LastInsertedId);
        await LogAdminActivityAsync("Created", "Owner", ownerId, $"Created owner {ownerName}.", cancellationToken);
        return new AdminOwner(ownerId, ownerName, email, true);
    }

    public async Task UpdateOwnerAsync(
        int ownerId,
        string ownerName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureOwnerStatusColumnAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE site_owner
            SET owner_name = @name, owner_email = @email, password = @password
            WHERE owner_id = @ownerId;
            """;
        command.Parameters.AddWithValue("@name", ownerName);
        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@password", password);
        command.Parameters.AddWithValue("@ownerId", ownerId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Site owner could not be updated.");
        }

        await LogAdminActivityAsync("Updated", "Owner", ownerId, $"Updated owner {ownerName}.", cancellationToken);
    }

    public async Task UpdateOwnerStatusAsync(
        int ownerId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureOwnerStatusColumnAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE site_owner
            SET is_active = @isActive
            WHERE owner_id = @ownerId;
            """;
        command.Parameters.AddWithValue("@isActive", isActive);
        command.Parameters.AddWithValue("@ownerId", ownerId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Site owner status could not be updated.");
        }

        await LogAdminActivityAsync(
            "Updated",
            "Owner",
            ownerId,
            $"Set owner status to {(isActive ? "Active" : "Inactive")}.",
            cancellationToken);
    }

    public async Task DeleteOwnerAsync(int ownerId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM site_owner WHERE owner_id = @ownerId;";
        command.Parameters.AddWithValue("@ownerId", ownerId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Site owner could not be deleted.");
        }

        await LogAdminActivityAsync("Deleted", "Owner", ownerId, "Deleted owner.", cancellationToken);
    }

    private static AdminOwner ReadOwner(MySqlDataReader reader) => new(
        reader.GetInt32("owner_id"),
        GetNullableString(reader, "owner_name"),
        GetNullableString(reader, "owner_email"),
        reader.GetBoolean("is_active"));
}
