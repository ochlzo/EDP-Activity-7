using edp_gui_app;
using MySqlConnector;

namespace edp_gui_app.Tests;

[TestClass]
public sealed class SiteOwnerAuthStatusTests
{
    private const string ConnectionString =
        "Server=127.0.0.1;Port=3306;Database=site_management;User ID=root;Password=NewStrongPassword123!;SslMode=None;";

    [TestMethod]
    public async Task AuthenticateAsync_ReturnsNull_WhenOwnerIsInactive()
    {
        var email = $"codex_status_{Guid.NewGuid():N}@example.com";
        const string password = "SamplePassword123!";
        await InsertOwnerAsync(email, password);

        try
        {
            var service = new SiteOwnerAuthService(ConnectionString);
            Assert.IsNotNull(await service.AuthenticateAsync(email, password));

            await SetOwnerActiveAsync(email, false);

            var owner = await service.AuthenticateAsync(email, password);

            Assert.IsNull(owner);
        }
        finally
        {
            await DeleteOwnerAsync(email);
        }
    }

    private static async Task InsertOwnerAsync(string email, string password)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO site_owner (owner_name, owner_email, password)
            VALUES (@name, @email, @password);
            """;
        command.Parameters.AddWithValue("@name", "Codex Status Owner");
        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@password", password);

        await command.ExecuteNonQueryAsync();
    }

    private static async Task SetOwnerActiveAsync(string email, bool isActive)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE site_owner SET is_active = @isActive WHERE owner_email = @email;";
        command.Parameters.AddWithValue("@isActive", isActive);
        command.Parameters.AddWithValue("@email", email);

        await command.ExecuteNonQueryAsync();
    }

    private static async Task DeleteOwnerAsync(string email)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM site_owner WHERE owner_email = @email;";
        command.Parameters.AddWithValue("@email", email);

        await command.ExecuteNonQueryAsync();
    }
}
