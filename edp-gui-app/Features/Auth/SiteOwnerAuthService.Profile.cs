using MySqlConnector;

namespace edp_gui_app;

public sealed partial class SiteOwnerAuthService
{
    public async Task<SiteOwner> LoadOwnerProfileAsync(
        int ownerId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureOwnerProfileColumnsAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT owner_id, owner_name, owner_email, contact_number
            FROM site_owner
            WHERE owner_id = @ownerId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@ownerId", ownerId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Owner profile could not be found.");
        }

        return ReadSiteOwner(reader);
    }

    public async Task UpdateOwnerDetailsAsync(
        int ownerId,
        string ownerName,
        string contactNumber,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureOwnerProfileColumnsAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE site_owner
            SET owner_name = @ownerName,
                contact_number = @contactNumber
            WHERE owner_id = @ownerId;
            """;
        command.Parameters.AddWithValue("@ownerName", ownerName);
        command.Parameters.AddWithValue("@contactNumber", contactNumber);
        command.Parameters.AddWithValue("@ownerId", ownerId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Owner profile could not be updated.");
        }
    }

    public async Task UpdateOwnerEmailAsync(
        int ownerId,
        string currentPassword,
        string email,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureOwnerProfileColumnsAsync(connection, cancellationToken);

        await EnsureCurrentPasswordAsync(connection, ownerId, currentPassword, cancellationToken);
        if (await OwnerEmailExistsAsync(connection, ownerId, email, cancellationToken))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE site_owner
            SET owner_email = @email
            WHERE owner_id = @ownerId;
            """;
        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@ownerId", ownerId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Owner email could not be updated.");
        }
    }

    public async Task UpdateOwnerPasswordAsync(
        int ownerId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureOwnerProfileColumnsAsync(connection, cancellationToken);

        await EnsureCurrentPasswordAsync(connection, ownerId, currentPassword, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE site_owner
            SET password = @password
            WHERE owner_id = @ownerId;
            """;
        command.Parameters.AddWithValue("@password", PasswordHasher.Hash(newPassword));
        command.Parameters.AddWithValue("@ownerId", ownerId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Owner password could not be updated.");
        }
    }

    private static async Task EnsureOwnerProfileColumnsAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await EnsureOwnerStatusColumnAsync(connection, cancellationToken);
        await AddColumnIfMissingAsync(
            connection,
            "site_owner",
            "contact_number",
            "VARCHAR(80) NOT NULL DEFAULT ''",
            cancellationToken);
    }

    private static async Task<bool> OwnerEmailExistsAsync(
        MySqlConnection connection,
        int ownerId,
        string email,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT 1
            FROM site_owner
            WHERE owner_email = @email AND owner_id <> @ownerId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@ownerId", ownerId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    private static async Task EnsureCurrentPasswordAsync(
        MySqlConnection connection,
        int ownerId,
        string currentPassword,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT password
            FROM site_owner
            WHERE owner_id = @ownerId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@ownerId", ownerId);

        var storedPassword = await command.ExecuteScalarAsync(cancellationToken);
        if (storedPassword is null)
        {
            throw new InvalidOperationException("Owner profile could not be found.");
        }

        if (!PasswordHasher.Verify(currentPassword, Convert.ToString(storedPassword)!))
        {
            throw new InvalidOperationException("Current password is incorrect.");
        }
    }

    private static SiteOwner ReadSiteOwner(MySqlDataReader reader)
    {
        return new SiteOwner(
            reader.GetInt32("owner_id"),
            reader.GetString("owner_name"),
            reader.GetString("owner_email"),
            reader.GetString("contact_number"));
    }
}
