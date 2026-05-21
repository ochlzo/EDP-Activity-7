using edp_gui_admin;
using MySqlConnector;

namespace edp_gui_admin.Tests;

[TestClass]
public sealed class AdminRecordServiceTests
{
    private const string ConnectionString =
        "Server=127.0.0.1;Port=3306;Database=site_management;User ID=root;Password=NewStrongPassword123!;SslMode=None;";

    [TestMethod]
    public async Task OwnerSiteRiserAndRoomCrud_AffectsExpectedRows()
    {
        var service = new AdminRecordService(ConnectionString);
        var email = $"owner_{Guid.NewGuid():N}@example.com";
        int? ownerId = null;
        int? siteId = null;
        int? riserId = null;
        int? roomId = null;
        int? tenantId = null;
        int? documentId = null;

        try
        {
            var owner = await service.CreateOwnerAsync("Owner One", email, "Password123!");
            ownerId = owner.OwnerId;
            Assert.AreEqual(email, owner.OwnerEmail);
            Assert.IsTrue(owner.IsActive);

            var site = await service.CreateSiteAsync("Site One", owner.OwnerId);
            siteId = site.SiteId;
            Assert.AreEqual(owner.OwnerId, site.OwnerId);

            var riser = await service.CreateRiserAsync("Riser One", site.SiteId);
            riserId = riser.RiserId;
            Assert.AreEqual(site.SiteId, riser.SiteId);

            var room = await service.CreateRoomAsync("Room One", riser.RiserId);
            roomId = room.RoomId;
            Assert.AreEqual(riser.RiserId, room.RiserId);

            var tenant = await service.CreateTenantAsync("Tenant One");
            tenantId = tenant.TenantId;
            Assert.AreEqual("Tenant One", tenant.TenantName);

            var document = await service.CreateDocumentAsync("Document One", tenant.TenantId);
            documentId = document.DocumentId;
            Assert.AreEqual(tenant.TenantId, document.TenantId);

            await service.UpdateOwnerAsync(owner.OwnerId, "Owner Updated", email, "Password123!");
            await service.UpdateOwnerStatusAsync(owner.OwnerId, false);
            await service.UpdateSiteAsync(site.SiteId, "Site Updated", owner.OwnerId);
            await service.UpdateRiserAsync(riser.RiserId, "Riser Updated", site.SiteId);
            await service.UpdateRoomAsync(room.RoomId, "Room Updated", riser.RiserId, null);
            await service.UpdateTenantAsync(tenant.TenantId, "Tenant Updated");
            await service.UpdateDocumentAsync(document.DocumentId, "Document Updated", tenant.TenantId);

            CollectionAssert.Contains(
                (await service.LoadOwnersAsync()).Select(row => row.OwnerName).ToArray(),
                "Owner Updated");
            Assert.AreEqual("Inactive", (await service.LoadOwnersAsync()).Single(row => row.OwnerId == owner.OwnerId).Status);
            CollectionAssert.Contains(
                (await service.LoadSitesAsync()).Select(row => row.SiteName).ToArray(),
                "Site Updated");
            CollectionAssert.Contains(
                (await service.LoadRisersAsync()).Select(row => row.RiserName).ToArray(),
                "Riser Updated");
            CollectionAssert.Contains(
                (await service.LoadRoomsAsync()).Select(row => row.RoomName).ToArray(),
                "Room Updated");
            CollectionAssert.Contains(
                (await service.LoadTenantsAsync()).Select(row => row.TenantName).ToArray(),
                "Tenant Updated");
            CollectionAssert.Contains(
                (await service.LoadDocumentsAsync()).Select(row => row.DocumentName).ToArray(),
                "Document Updated");
        }
        finally
        {
            await DeleteDocumentAsync(documentId);
            await DeleteRoomAsync(roomId);
            await DeleteRiserAsync(riserId);
            await DeleteSiteAsync(siteId);
            await DeleteTenantAsync(tenantId);
            await DeleteOwnerAsync(ownerId);
        }
    }

    [TestMethod]
    public async Task CreateDocumentAsync_SavesComplianceFields()
    {
        var service = new AdminRecordService(ConnectionString);
        int? tenantId = null;
        int? documentId = null;
        var today = DateTime.UtcNow.Date;

        try
        {
            var tenant = await service.CreateTenantAsync("Compliance Tenant");
            tenantId = tenant.TenantId;

            var document = await service.CreateDocumentAsync(
                "Lease",
                tenant.TenantId,
                "Lease",
                "Active",
                "Signed");
            documentId = document.DocumentId;

            Assert.AreEqual("Lease", document.DocumentType);
            Assert.AreEqual("Active", document.DocumentStatus);
            Assert.AreEqual(today, document.IssuedAt?.Date);
            Assert.IsNull(document.SubmittedAt);
            Assert.AreEqual("Signed", document.Notes);
        }
        finally
        {
            await DeleteDocumentAsync(documentId);
            await DeleteTenantAsync(tenantId);
        }
    }

    [TestMethod]
    public async Task CreateTenantAsync_SavesTenantDetails()
    {
        var service = new AdminRecordService(ConnectionString);
        int? tenantId = null;

        try
        {
            var tenant = await service.CreateTenantAsync(
                "Tenant Details",
                "tenant@example.com",
                "123 Main St",
                "555-0100");
            tenantId = tenant.TenantId;

            Assert.AreEqual("tenant@example.com", tenant.TenantEmail);
            Assert.AreEqual("123 Main St", tenant.TenantAddress);
            Assert.AreEqual("555-0100", tenant.TenantContactNumber);

            await service.UpdateTenantAsync(
                tenant.TenantId,
                "Tenant Updated",
                "updated@example.com",
                "456 Oak Ave",
                "555-0200");

            var loaded = (await service.LoadTenantsAsync()).Single(row => row.TenantId == tenant.TenantId);
            Assert.AreEqual("Tenant Updated", loaded.TenantName);
            Assert.AreEqual("updated@example.com", loaded.TenantEmail);
            Assert.AreEqual("456 Oak Ave", loaded.TenantAddress);
            Assert.AreEqual("555-0200", loaded.TenantContactNumber);
        }
        finally
        {
            await DeleteTenantAsync(tenantId);
        }
    }

    private static async Task DeleteOwnerAsync(int? ownerId) =>
        await DeleteByIdAsync("site_owner", "owner_id", ownerId);

    private static async Task DeleteSiteAsync(int? siteId) =>
        await DeleteByIdAsync("site", "site_id", siteId);

    private static async Task DeleteRiserAsync(int? riserId) =>
        await DeleteByIdAsync("riser", "riser_id", riserId);

    private static async Task DeleteRoomAsync(int? roomId) =>
        await DeleteByIdAsync("room", "room_id", roomId);

    private static async Task DeleteTenantAsync(int? tenantId) =>
        await DeleteByIdAsync("tenant", "tenant_id", tenantId);

    private static async Task DeleteDocumentAsync(int? documentId) =>
        await DeleteByIdAsync("document", "document_id", documentId);

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
}
