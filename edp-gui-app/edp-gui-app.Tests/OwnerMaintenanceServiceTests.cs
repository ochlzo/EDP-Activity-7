using edp_gui_app;
using MySqlConnector;

namespace edp_gui_app.Tests;

[TestClass]
public sealed class OwnerMaintenanceServiceTests
{
    private const string ConnectionString =
        "Server=127.0.0.1;Port=3306;Database=site_management;User ID=root;Password=NewStrongPassword123!;SslMode=None;";

    [TestMethod]
    public async Task CreateMaintenanceTicketAsync_CreatesOpenTicketForOwnedSite()
    {
        var email = $"owner_maintenance_{Guid.NewGuid():N}@example.com";
        var ownerId = await InsertOwnerAsync(email);
        var siteId = await InsertSiteAsync(ownerId, "Maintenance Site");
        var service = new OwnerMaintenanceService(ConnectionString);

        try
        {
            var ticket = await service.CreateMaintenanceTicketAsync(
                ownerId,
                siteId,
                null,
                null,
                "Leak",
                "Water leak",
                "High");

            Assert.AreEqual("Open", ticket.Status);
            Assert.AreEqual("Leak", ticket.Title);
            Assert.AreEqual("Water leak", ticket.Description);
            Assert.AreEqual("High", ticket.Priority);
            Assert.IsNull(ticket.ResolvedAt);
            Assert.AreEqual(string.Empty, ticket.Notes);
        }
        finally
        {
            await DeleteSiteAsync(siteId);
            await DeleteOwnerAsync(email);
        }
    }

    private static async Task<int> InsertOwnerAsync(string email)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO site_owner (owner_name, owner_email, password)
            VALUES ('Maintenance Owner', @email, 'Password123!');
            """;
        command.Parameters.AddWithValue("@email", email);
        await command.ExecuteNonQueryAsync();
        return Convert.ToInt32(command.LastInsertedId);
    }

    private static async Task<int> InsertSiteAsync(int ownerId, string siteName)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO site (site_name, owner_id) VALUES (@siteName, @ownerId);";
        command.Parameters.AddWithValue("@siteName", siteName);
        command.Parameters.AddWithValue("@ownerId", ownerId);
        await command.ExecuteNonQueryAsync();
        return Convert.ToInt32(command.LastInsertedId);
    }

    private static Task DeleteSiteAsync(int siteId) => DeleteByIdAsync("site", "site_id", siteId);

    private static async Task DeleteOwnerAsync(string email)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM site_owner WHERE owner_email = @email;";
        command.Parameters.AddWithValue("@email", email);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DeleteByIdAsync(string tableName, string keyName, int id)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {tableName} WHERE {keyName} = @id;";
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync();
    }
}
