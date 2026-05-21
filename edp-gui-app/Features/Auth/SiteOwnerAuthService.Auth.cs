using MySqlConnector;

namespace edp_gui_app;

public sealed partial class SiteOwnerAuthService
{
    public SiteOwnerAuthService(string connectionString)
        : this(
            connectionString,
            GmailPasswordResetEmailSender.FromEnvironment(),
            new PasswordResetCodeGenerator(),
            new SystemClock())
    {
    }

    public async Task<SiteOwner?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureOwnerProfileColumnsAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT owner_id, owner_name, owner_email, contact_number, password
            FROM site_owner
            WHERE owner_email = @email AND is_active = 1
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@email", email);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var storedPassword = reader.GetString("password");
        if (!PasswordHasher.Verify(password, storedPassword))
        {
            return null;
        }

        var owner = new SiteOwner(
            reader.GetInt32("owner_id"),
            reader.GetString("owner_name"),
            reader.GetString("owner_email"),
            reader.GetString("contact_number"));

        await reader.DisposeAsync();
        if (PasswordHasher.NeedsRehash(storedPassword))
        {
            await UpdateOwnerPasswordAsync(connection, owner.OwnerId, PasswordHasher.Hash(password), cancellationToken);
        }

        return owner;
    }

    public async Task<CreateSiteOwnerResult> CreateOwnerAsync(
        string ownerName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureOwnerProfileColumnsAsync(connection, cancellationToken);

        if (await OwnerEmailExistsAsync(connection, email, cancellationToken))
        {
            return new CreateSiteOwnerResult(CreateSiteOwnerStatus.EmailAlreadyExists, null);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO site_owner (owner_name, owner_email, contact_number, password, is_active)
            VALUES (@name, @email, @contactNumber, @password, 1);
            """;
        command.Parameters.AddWithValue("@name", ownerName);
        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@contactNumber", string.Empty);
        command.Parameters.AddWithValue("@password", PasswordHasher.Hash(password));

        await command.ExecuteNonQueryAsync(cancellationToken);

        return new CreateSiteOwnerResult(
            CreateSiteOwnerStatus.Created,
            new SiteOwner(Convert.ToInt32(command.LastInsertedId), ownerName, email, string.Empty));
    }

    private static async Task<bool> OwnerEmailExistsAsync(
        MySqlConnection connection,
        string email,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM site_owner WHERE owner_email = @email LIMIT 1;";
        command.Parameters.AddWithValue("@email", email);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    private static async Task EnsureOwnerStatusColumnAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
                AND table_name = 'site_owner'
                AND column_name = 'is_active';
            """;

        var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync(cancellationToken)) > 0;
        if (exists)
        {
            return;
        }

        await using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = """
            ALTER TABLE site_owner
            ADD COLUMN is_active TINYINT(1) NOT NULL DEFAULT 1;
            """;
        await alterCommand.ExecuteNonQueryAsync(cancellationToken);
    }
}
