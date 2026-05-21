using edp_gui_app;
using MySqlConnector;

namespace edp_gui_app.Tests;

[TestClass]
public sealed class SiteOwnerRoomTenantServiceTests
{
    private const string ConnectionString =
        "Server=127.0.0.1;Port=3306;Database=site_management;User ID=root;Password=NewStrongPassword123!;SslMode=None;";

    [TestMethod]
    public async Task CreateTenantInRoomAsync_CreatesTenantAndAssignsOwnedVacantRoom()
    {
        var ownerEmail = BuildTestEmail();
        const string password = "SamplePassword123!";
        var ownerId = await InsertOwnerAsync(ownerEmail, password);
        var siteIds = new List<int>();
        var riserIds = new List<int>();
        var roomIds = new List<int>();
        var tenantIds = new List<int>();

        try
        {
            var before = DateTime.Now;
            var siteId = await InsertSiteAsync(ownerId, "North Tower");
            siteIds.Add(siteId);

            var riserId = await InsertRiserAsync(siteId, "Riser A");
            riserIds.Add(riserId);

            var roomId = await InsertRoomAsync(riserId, "Room 101");
            roomIds.Add(roomId);

            var service = new SiteOwnerAuthService(ConnectionString);

            var room = await service.CreateTenantInRoomAsync(
                roomId,
                siteId,
                ownerId,
                "Acme Tenant",
                "tenant@example.com",
                "101 North Tower",
                "09171234567");
            tenantIds.Add(room.TenantId!.Value);

            Assert.AreEqual(roomId, room.RoomId);
            Assert.AreEqual("Room 101", room.RoomName);
            Assert.AreEqual("Acme Tenant", room.TenantName);
            Assert.AreEqual("tenant@example.com", room.TenantEmail);
            Assert.AreEqual("Occupied", room.Occupancy);

            var transaction = await LoadLatestOccupancyTransactionAsync(roomId);
            Assert.AreEqual("Assigned", transaction.TransactionType);
            Assert.IsTrue(transaction.Date >= before.AddSeconds(-1));
            Assert.IsTrue(transaction.Date <= DateTime.Now.AddSeconds(5));

            var rooms = await service.LoadRoomsBySiteAsync(siteId, ownerId);
            Assert.AreEqual("Acme Tenant", rooms.Single().TenantName);
            Assert.AreEqual("tenant@example.com", rooms.Single().TenantEmail);

            var tenant = await service.LoadTenantDetailsAsync(room.TenantId.Value, siteId, ownerId);
            Assert.AreEqual("Acme Tenant", tenant.Name);
            Assert.AreEqual("tenant@example.com", tenant.Email);
            Assert.AreEqual("101 North Tower", tenant.Address);
            Assert.AreEqual("09171234567", tenant.ContactNumber);
        }
        finally
        {
            await DeleteRoomsAsync(roomIds);
            await DeleteTenantsAsync(tenantIds);
            await DeleteRisersAsync(riserIds);
            await DeleteSitesAsync(siteIds);
            await DeleteOwnerAsync(ownerEmail);
        }
    }

    [TestMethod]
    public async Task UpdateTenantDetailsAsync_UpdatesOwnedTenantProfile()
    {
        var ownerEmail = BuildTestEmail();
        const string password = "SamplePassword123!";
        var ownerId = await InsertOwnerAsync(ownerEmail, password);
        var siteIds = new List<int>();
        var riserIds = new List<int>();
        var roomIds = new List<int>();
        var tenantIds = new List<int>();

        try
        {
            var siteId = await InsertSiteAsync(ownerId, "North Tower");
            siteIds.Add(siteId);
            var riserId = await InsertRiserAsync(siteId, "Riser A");
            riserIds.Add(riserId);
            var roomId = await InsertRoomAsync(riserId, "Room 101");
            roomIds.Add(roomId);

            var service = new SiteOwnerAuthService(ConnectionString);
            var room = await service.CreateTenantInRoomAsync(
                roomId,
                siteId,
                ownerId,
                "Acme Tenant",
                "tenant@example.com",
                "101 North Tower",
                "09171234567");
            tenantIds.Add(room.TenantId!.Value);

            await service.UpdateTenantDetailsAsync(
                room.TenantId.Value,
                siteId,
                ownerId,
                "Updated Tenant",
                "updated@example.com",
                "202 North Tower",
                "09998887777");

            var tenant = await service.LoadTenantDetailsAsync(room.TenantId.Value, siteId, ownerId);
            Assert.AreEqual("Updated Tenant", tenant.Name);
            Assert.AreEqual("updated@example.com", tenant.Email);
            Assert.AreEqual("202 North Tower", tenant.Address);
            Assert.AreEqual("09998887777", tenant.ContactNumber);

            var rooms = await service.LoadRoomsByRiserAsync(riserId, siteId, ownerId);
            Assert.AreEqual("Updated Tenant", rooms.Single().TenantName);
            Assert.AreEqual("updated@example.com", rooms.Single().TenantEmail);
        }
        finally
        {
            await DeleteRoomsAsync(roomIds);
            await DeleteTenantsAsync(tenantIds);
            await DeleteRisersAsync(riserIds);
            await DeleteSitesAsync(siteIds);
            await DeleteOwnerAsync(ownerEmail);
        }
    }

    private static string BuildTestEmail() => $"codex_{Guid.NewGuid():N}@example.com";

    private static async Task<int> InsertOwnerAsync(string email, string password)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO site_owner (owner_name, owner_email, password)
            VALUES (@name, @email, @password);
            """;
        command.Parameters.AddWithValue("@name", "Codex Test Owner");
        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@password", password);

        await command.ExecuteNonQueryAsync();
        return Convert.ToInt32(command.LastInsertedId);
    }

    private static async Task<int> InsertSiteAsync(int ownerId, string siteName)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO site (site_name, owner_id)
            VALUES (@siteName, @ownerId);
            """;
        command.Parameters.AddWithValue("@siteName", siteName);
        command.Parameters.AddWithValue("@ownerId", ownerId);

        await command.ExecuteNonQueryAsync();
        return Convert.ToInt32(command.LastInsertedId);
    }

    private static async Task<int> InsertRiserAsync(int siteId, string riserName)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO riser (riser_name, site_id)
            VALUES (@riserName, @siteId);
            """;
        command.Parameters.AddWithValue("@riserName", riserName);
        command.Parameters.AddWithValue("@siteId", siteId);

        await command.ExecuteNonQueryAsync();
        return Convert.ToInt32(command.LastInsertedId);
    }

    private static async Task<int> InsertRoomAsync(int riserId, string roomName)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO room (room_name, riser_id, tenant_id)
            VALUES (@roomName, @riserId, NULL);
            """;
        command.Parameters.AddWithValue("@roomName", roomName);
        command.Parameters.AddWithValue("@riserId", riserId);

        await command.ExecuteNonQueryAsync();
        return Convert.ToInt32(command.LastInsertedId);
    }

    private static Task DeleteRoomsAsync(IEnumerable<int> roomIds) => DeleteByIdsAsync("room", "room_id", roomIds);

    private static Task DeleteTenantsAsync(IEnumerable<int> tenantIds) => DeleteByIdsAsync("tenant", "tenant_id", tenantIds);

    private static Task DeleteRisersAsync(IEnumerable<int> riserIds) => DeleteByIdsAsync("riser", "riser_id", riserIds);

    private static Task DeleteSitesAsync(IEnumerable<int> siteIds) => DeleteByIdsAsync("site", "site_id", siteIds);

    private static async Task DeleteOwnerAsync(string email)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM site_owner WHERE owner_email = @email;";
        command.Parameters.AddWithValue("@email", email);

        await command.ExecuteNonQueryAsync();
    }

    private static async Task DeleteByIdsAsync(string tableName, string columnName, IEnumerable<int> ids)
    {
        var values = ids.ToArray();
        if (values.Length == 0)
        {
            return;
        }

        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {tableName} WHERE {columnName} IN ({string.Join(", ", values)});";

        await command.ExecuteNonQueryAsync();
    }

    private static async Task<(string TransactionType, DateTime Date)> LoadLatestOccupancyTransactionAsync(int roomId)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT transaction_type, effective_at
            FROM room_occupancy_transaction
            WHERE room_id = @roomId
            ORDER BY occupancy_transaction_id DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@roomId", roomId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException("Occupancy transaction was not created.");
        }

        return (
            reader.GetString("transaction_type"),
            reader.GetDateTime("effective_at"));
    }

}
