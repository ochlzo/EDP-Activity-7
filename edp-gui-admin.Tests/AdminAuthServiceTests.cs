using edp_gui_admin;
using MySqlConnector;

[assembly: DoNotParallelize]

namespace edp_gui_admin.Tests;

[TestClass]
public sealed class AdminAuthServiceTests
{
    private const string ConnectionString =
        "Server=127.0.0.1;Port=3306;Database=site_management;User ID=root;Password=NewStrongPassword123!;SslMode=None;";

    [TestMethod]
    public async Task EnsureSchemaAsync_SeedsExactlyOneFixedAdmin()
    {
        var service = new AdminAuthService(ConnectionString);

        await service.EnsureSchemaAsync();

        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT admin_id, admin_username, admin_password
            FROM admin_user
            ORDER BY admin_id;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual(1, reader.GetInt32("admin_id"));
        Assert.AreEqual("admin", reader.GetString("admin_username"));
        Assert.AreEqual("Admin123456!", reader.GetString("admin_password"));
        Assert.IsFalse(await reader.ReadAsync());
    }

    [TestMethod]
    public async Task AuthenticateAsync_ReturnsAdmin_WhenFixedCredentialsMatch()
    {
        var service = new AdminAuthService(ConnectionString);
        await service.EnsureSchemaAsync();

        var authenticated = await service.AuthenticateAsync("admin", "Admin123456!");

        Assert.IsNotNull(authenticated);
        Assert.AreEqual(1, authenticated.AdminId);
        Assert.AreEqual("admin", authenticated.AdminUsername);
    }

    [TestMethod]
    public async Task AuthenticateAsync_ReturnsNull_WhenCredentialsDoNotMatch()
    {
        var service = new AdminAuthService(ConnectionString);
        await service.EnsureSchemaAsync();

        var authenticated = await service.AuthenticateAsync("admin", "WrongPassword123!");

        Assert.IsNull(authenticated);
    }
}
