using edp_gui_admin;
using MySqlConnector;

namespace edp_gui_admin.Tests;

[TestClass]
public sealed class AdminTransactionServiceMaintenanceTests
{
    private const string ConnectionString =
        "Server=127.0.0.1;Port=3306;Database=site_management;User ID=root;Password=NewStrongPassword123!;SslMode=None;";

    [TestMethod]
    public async Task UpdateMaintenanceTicketStatusAsync_WritesHistory()
    {
        var ids = await SeedMaintenanceTicketAsync();
        var service = new AdminTransactionService(ConnectionString);

        try
        {
            await service.UpdateMaintenanceTicketStatusAsync(
                ids.TicketId!.Value,
                "In Progress",
                "admin",
                "Assigned to technician");

            var history = await service.LoadMaintenanceHistoryAsync(ids.TicketId.Value);
            var entry = history.Single();

            Assert.AreEqual("Open", entry.OldStatus);
            Assert.AreEqual("In Progress", entry.NewStatus);
            Assert.AreEqual("Assigned to technician", entry.Notes);
        }
        finally
        {
            await DeleteSeedAsync(ids);
        }
    }

    [TestMethod]
    public async Task ResolveMaintenanceTicketAsync_StoresNotesAndResolvedAt()
    {
        var ids = await SeedMaintenanceTicketAsync();
        var service = new AdminTransactionService(ConnectionString);
        var before = DateTime.Now;

        try
        {
            await service.ResolveMaintenanceTicketAsync(
                ids.TicketId!.Value,
                "Checked wiring and closed ticket",
                "admin");

            var ticket = (await service.LoadMaintenanceTicketsAsync())
                .Single(row => row.TicketId == ids.TicketId.Value);
            var history = await service.LoadMaintenanceHistoryAsync(ids.TicketId.Value);
            var entry = history.Single();

            Assert.AreEqual("Resolved", ticket.Status);
            Assert.AreEqual("Ticket", ticket.Title);
            Assert.AreEqual("Description", ticket.Description);
            Assert.AreEqual("Checked wiring and closed ticket", ticket.Notes);
            Assert.IsTrue(ticket.ResolvedAt >= before.AddSeconds(-1));
            Assert.IsTrue(ticket.ResolvedAt <= DateTime.Now.AddSeconds(5));
            Assert.AreEqual("Open", entry.OldStatus);
            Assert.AreEqual("Resolved", entry.NewStatus);
            Assert.AreEqual("Checked wiring and closed ticket", entry.Notes);
        }
        finally
        {
            await DeleteSeedAsync(ids);
        }
    }

    [TestMethod]
    public async Task UpdateMaintenanceTicketAsync_UpdatesEditableFields()
    {
        var ids = await SeedMaintenanceTicketAsync();
        var service = new AdminTransactionService(ConnectionString);

        try
        {
            await service.UpdateMaintenanceTicketAsync(
                ids.TicketId!.Value,
                "Repaired breaker",
                "High",
                "In Progress",
                "Waiting on final inspection",
                "admin");

            var ticket = (await service.LoadMaintenanceTicketsAsync())
                .Single(row => row.TicketId == ids.TicketId.Value);
            var history = await service.LoadMaintenanceHistoryAsync(ids.TicketId.Value);
            var entry = history.Single();

            Assert.AreEqual("Repaired breaker", ticket.Title);
            Assert.AreEqual("High", ticket.Priority);
            Assert.AreEqual("In Progress", ticket.Status);
            Assert.AreEqual("Waiting on final inspection", ticket.Notes);
            Assert.IsNull(ticket.ResolvedAt);
            Assert.AreEqual("Open", entry.OldStatus);
            Assert.AreEqual("In Progress", entry.NewStatus);
            Assert.AreEqual("Waiting on final inspection", entry.Notes);
        }
        finally
        {
            await DeleteSeedAsync(ids);
        }
    }

    private static async Task<SeedIds> SeedMaintenanceTicketAsync()
    {
        var records = new AdminRecordService(ConnectionString);
        var owner = await records.CreateOwnerAsync("Ticket Owner", $"ticket_{Guid.NewGuid():N}@example.com", "Password123!");
        var site = await records.CreateSiteAsync("Ticket Site", owner.OwnerId);
        var service = new AdminTransactionService(ConnectionString);
        await service.EnsureSchemaAsync();

        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO maintenance_ticket
                (site_id, requested_by_owner_id, title, description, priority, status)
            VALUES
                (@siteId, @ownerId, 'Ticket', 'Description', 'Normal', 'Open');
            """;
        command.Parameters.AddWithValue("@siteId", site.SiteId);
        command.Parameters.AddWithValue("@ownerId", owner.OwnerId);
        await command.ExecuteNonQueryAsync();

        return new SeedIds(owner.OwnerId, site.SiteId, Convert.ToInt32(command.LastInsertedId));
    }

    private static async Task DeleteSeedAsync(SeedIds ids)
    {
        await DeleteByIdAsync("maintenance_ticket", "ticket_id", ids.TicketId);
        await DeleteByIdAsync("site", "site_id", ids.SiteId);
        await DeleteByIdAsync("site_owner", "owner_id", ids.OwnerId);
    }

    private static async Task DeleteByIdAsync(string tableName, string keyName, int? id)
    {
        if (id is null)
        {
            return;
        }

        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {tableName} WHERE {keyName} = @id;";
        command.Parameters.AddWithValue("@id", id.Value);
        await command.ExecuteNonQueryAsync();
    }

    private sealed record SeedIds(
        int OwnerId,
        int? SiteId,
        int? TicketId);
}
