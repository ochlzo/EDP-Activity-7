using edp_gui_app;
using MySqlConnector;

namespace edp_gui_app.Tests;

[TestClass]
public sealed class SiteOwnerRoomReassignmentTests
{
    private const string ConnectionString =
        "Server=127.0.0.1;Port=3306;Database=site_management;User ID=root;Password=NewStrongPassword123!;SslMode=None;";

    [TestMethod]
    public async Task ReplaceTenantInRoomAsync_UpdatesRoomAndKeepsOccupancyHistory()
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
            var initialRoom = await service.CreateTenantInRoomAsync(
                roomId,
                siteId,
                ownerId,
                "Original Tenant",
                "original@example.com",
                "101 North Tower",
                "09170000001");
            tenantIds.Add(initialRoom.TenantId!.Value);

            var before = DateTime.Now;
            var replacementRoom = await service.ReplaceTenantInRoomAsync(
                roomId,
                siteId,
                ownerId,
                "Replacement Tenant",
                "replacement@example.com",
                "202 North Tower",
                "09170000002");
            tenantIds.Add(replacementRoom.TenantId!.Value);

            Assert.AreEqual(roomId, replacementRoom.RoomId);
            Assert.AreEqual("Replacement Tenant", replacementRoom.TenantName);
            Assert.AreEqual("replacement@example.com", replacementRoom.TenantEmail);

            var room = (await service.LoadRoomsBySiteAsync(siteId, ownerId))
                .Single(row => row.RoomId == roomId);
            Assert.AreEqual("Replacement Tenant", room.TenantName);
            Assert.AreEqual("replacement@example.com", room.TenantEmail);

            var transactions = await LoadOccupancyTransactionsAsync(roomId);
            Assert.AreEqual(2, transactions.Count);
            Assert.AreEqual("Assigned", transactions[0].TransactionType);
            Assert.AreEqual("Replaced", transactions[1].TransactionType);
            Assert.AreEqual("Original Tenant", transactions[0].TenantName);
            Assert.AreEqual("Replacement Tenant", transactions[1].TenantName);
            Assert.IsTrue(transactions[1].Date >= before.AddSeconds(-1));
            Assert.IsTrue(transactions[1].Date <= DateTime.Now.AddSeconds(5));
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

    private static async Task<IReadOnlyList<(string TransactionType, string TenantName, DateTime Date)>> LoadOccupancyTransactionsAsync(
        int roomId)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT tx.transaction_type, COALESCE(tenant.tenant_name, '') AS tenant_name, tx.effective_at
            FROM room_occupancy_transaction tx
            LEFT JOIN tenant ON tenant.tenant_id = tx.tenant_id
            WHERE tx.room_id = @roomId
            ORDER BY tx.occupancy_transaction_id ASC;
            """;
        command.Parameters.AddWithValue("@roomId", roomId);

        await using var reader = await command.ExecuteReaderAsync();
        var transactions = new List<(string TransactionType, string TenantName, DateTime Date)>();
        while (await reader.ReadAsync())
        {
            transactions.Add((
                reader.GetString("transaction_type"),
                reader.GetString("tenant_name"),
                reader.GetDateTime("effective_at")));
        }

        return transactions;
    }
}
