using MySqlConnector;

namespace edp_gui_app;

public sealed partial class SiteOwnerAuthService
{
    private const int PasswordResetMinutes = 15;

    private readonly IEmailSender? _emailSender;
    private readonly IPasswordResetCodeGenerator _passwordResetCodeGenerator;
    private readonly IClock _clock;

    public SiteOwnerAuthService(
        string connectionString,
        IEmailSender? emailSender,
        IPasswordResetCodeGenerator passwordResetCodeGenerator,
        IClock clock)
    {
        _connectionString = connectionString;
        _emailSender = emailSender;
        _passwordResetCodeGenerator = passwordResetCodeGenerator;
        _clock = clock;
    }

    public async Task<PasswordResetRequestStatus> RequestPasswordResetAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (_emailSender is null)
        {
            return PasswordResetRequestStatus.EmailNotConfigured;
        }

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsurePasswordResetTableAsync(connection, cancellationToken);

        var ownerId = await FindOwnerIdByEmailAsync(connection, email, cancellationToken);
        if (ownerId is null)
        {
            return PasswordResetRequestStatus.EmailDoesNotExist;
        }

        var code = _passwordResetCodeGenerator.CreateCode();
        var expiresAt = _clock.UtcNow.AddMinutes(PasswordResetMinutes);

        await InvalidateExistingResetCodesAsync(connection, ownerId.Value, cancellationToken);
        await InsertPasswordResetCodeAsync(
            connection,
            ownerId.Value,
            PasswordHasher.Hash(code),
            expiresAt,
            cancellationToken);

        await _emailSender.SendPasswordResetAsync(new PasswordResetEmailMessage(email, code), cancellationToken);
        return PasswordResetRequestStatus.SentIfAccountExists;
    }

    public async Task<PasswordResetStatus> ResetPasswordAsync(
        string email,
        string code,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsurePasswordResetTableAsync(connection, cancellationToken);

        var ownerId = await FindOwnerIdByEmailAsync(connection, email, cancellationToken);
        if (ownerId is null)
        {
            return PasswordResetStatus.InvalidOrExpired;
        }

        var resetId = await FindValidResetCodeIdAsync(connection, ownerId.Value, code, cancellationToken);
        if (resetId is null)
        {
            return PasswordResetStatus.InvalidOrExpired;
        }

        await UpdateOwnerPasswordAsync(connection, ownerId.Value, PasswordHasher.Hash(newPassword), cancellationToken);
        await MarkResetCodeUsedAsync(connection, resetId.Value, cancellationToken);
        return PasswordResetStatus.Reset;
    }

    public async Task<PasswordResetCodeStatus> VerifyPasswordResetCodeAsync(
        string email,
        string code,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsurePasswordResetTableAsync(connection, cancellationToken);

        var ownerId = await FindOwnerIdByEmailAsync(connection, email, cancellationToken);
        if (ownerId is null)
        {
            return PasswordResetCodeStatus.InvalidOrExpired;
        }

        var resetId = await FindValidResetCodeIdAsync(connection, ownerId.Value, code, cancellationToken);
        return resetId is null ? PasswordResetCodeStatus.InvalidOrExpired : PasswordResetCodeStatus.Valid;
    }

    private async Task<int?> FindOwnerIdByEmailAsync(
        MySqlConnection connection,
        string email,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT owner_id FROM site_owner WHERE owner_email = @email LIMIT 1;";
        command.Parameters.AddWithValue("@email", email);

        var ownerId = await command.ExecuteScalarAsync(cancellationToken);
        return ownerId is null ? null : Convert.ToInt32(ownerId);
    }

    private async Task<int?> FindValidResetCodeIdAsync(
        MySqlConnection connection,
        int ownerId,
        string code,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT reset_id, token_hash
            FROM site_owner_password_reset
            WHERE owner_id = @ownerId AND used_at IS NULL AND expires_at > @now
            ORDER BY reset_id DESC;
            """;
        command.Parameters.AddWithValue("@ownerId", ownerId);
        command.Parameters.AddWithValue("@now", _clock.UtcNow);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (PasswordHasher.Verify(code, reader.GetString("token_hash")))
            {
                return reader.GetInt32("reset_id");
            }
        }

        return null;
    }

    private static async Task EnsurePasswordResetTableAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS site_owner_password_reset (
                reset_id INT AUTO_INCREMENT PRIMARY KEY,
                owner_id INT NOT NULL,
                token_hash VARCHAR(255) NOT NULL,
                expires_at DATETIME NOT NULL,
                used_at DATETIME NULL,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                CONSTRAINT fk_site_owner_password_reset_owner
                    FOREIGN KEY (owner_id) REFERENCES site_owner(owner_id)
                    ON DELETE CASCADE
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertPasswordResetCodeAsync(
        MySqlConnection connection,
        int ownerId,
        string tokenHash,
        DateTime expiresAt,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO site_owner_password_reset (owner_id, token_hash, expires_at)
            VALUES (@ownerId, @tokenHash, @expiresAt);
            """;
        command.Parameters.AddWithValue("@ownerId", ownerId);
        command.Parameters.AddWithValue("@tokenHash", tokenHash);
        command.Parameters.AddWithValue("@expiresAt", expiresAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InvalidateExistingResetCodesAsync(
        MySqlConnection connection,
        int ownerId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE site_owner_password_reset
            SET used_at = UTC_TIMESTAMP()
            WHERE owner_id = @ownerId AND used_at IS NULL;
            """;
        command.Parameters.AddWithValue("@ownerId", ownerId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MarkResetCodeUsedAsync(
        MySqlConnection connection,
        int resetId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE site_owner_password_reset SET used_at = UTC_TIMESTAMP() WHERE reset_id = @resetId;";
        command.Parameters.AddWithValue("@resetId", resetId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpdateOwnerPasswordAsync(
        MySqlConnection connection,
        int ownerId,
        string password,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE site_owner SET password = @password WHERE owner_id = @ownerId;";
        command.Parameters.AddWithValue("@password", password);
        command.Parameters.AddWithValue("@ownerId", ownerId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
