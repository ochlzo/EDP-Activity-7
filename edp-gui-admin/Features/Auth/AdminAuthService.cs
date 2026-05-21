using MySqlConnector;

namespace edp_gui_admin;

public sealed class AdminAuthService
{
    private const int FixedAdminId = 1;
    private const string FixedUsername = "admin";
    private const string FixedPassword = "Admin123456!";

    private readonly string _connectionString;

    public AdminAuthService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await ExecuteAsync(connection, "DROP TABLE IF EXISTS admin_user;", cancellationToken);
        await ExecuteAsync(connection, """
            CREATE TABLE admin_user (
                admin_id INT PRIMARY KEY,
                admin_username VARCHAR(255) NOT NULL UNIQUE,
                admin_password VARCHAR(255) NOT NULL
            );
            """, cancellationToken);

        await using var seedCommand = connection.CreateCommand();
        seedCommand.CommandText = """
            INSERT INTO admin_user (admin_id, admin_username, admin_password)
            VALUES (@adminId, @username, @password);
            """;
        seedCommand.Parameters.AddWithValue("@adminId", FixedAdminId);
        seedCommand.Parameters.AddWithValue("@username", FixedUsername);
        seedCommand.Parameters.AddWithValue("@password", FixedPassword);
        await seedCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<AdminUser?> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT admin_id, admin_username
            FROM admin_user
            WHERE admin_id = @adminId
                AND admin_username = @username
                AND admin_password = @password
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@adminId", FixedAdminId);
        command.Parameters.AddWithValue("@username", username);
        command.Parameters.AddWithValue("@password", password);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new AdminUser(reader.GetInt32("admin_id"), reader.GetString("admin_username"))
            : null;
    }

    private static async Task ExecuteAsync(
        MySqlConnection connection,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
