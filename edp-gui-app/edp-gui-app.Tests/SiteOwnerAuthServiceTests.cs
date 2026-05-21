using edp_gui_app;
using MySqlConnector;

[assembly: DoNotParallelize]

namespace edp_gui_app.Tests;

[TestClass]
public sealed class SiteOwnerAuthServiceTests
{
    private const string ConnectionString =
        "Server=127.0.0.1;Port=3306;Database=site_management;User ID=root;Password=NewStrongPassword123!;SslMode=None;";

    [TestMethod]
    public async Task AuthenticateAsync_ReturnsOwner_WhenCredentialsMatch()
    {
        var email = BuildTestEmail();
        const string password = "SamplePassword123!";
        await InsertOwnerAsync(email, password);

        try
        {
            var service = new SiteOwnerAuthService(ConnectionString);

            var owner = await service.AuthenticateAsync(email, password);

            Assert.IsNotNull(owner);
            Assert.AreEqual(email, owner.OwnerEmail);
        }
        finally
        {
            await DeleteOwnerAsync(email);
        }
    }

    [TestMethod]
    public async Task AuthenticateAsync_ReturnsNull_WhenPasswordDoesNotMatch()
    {
        var email = BuildTestEmail();
        const string password = "SamplePassword123!";
        await InsertOwnerAsync(email, password);

        try
        {
            var service = new SiteOwnerAuthService(ConnectionString);

            var owner = await service.AuthenticateAsync(email, "WrongPassword123!");

            Assert.IsNull(owner);
        }
        finally
        {
            await DeleteOwnerAsync(email);
        }
    }

    [TestMethod]
    public async Task CreateOwnerAsync_CreatesOwner_WhenEmailIsNew()
    {
        var email = BuildTestEmail();
        const string password = "SamplePassword123!";
        const string ownerName = "Signup Owner";

        try
        {
            var service = new SiteOwnerAuthService(ConnectionString);

            var result = await service.CreateOwnerAsync(ownerName, email, password);

            Assert.AreEqual(CreateSiteOwnerStatus.Created, result.Status);
            Assert.IsNotNull(result.Owner);
            Assert.AreEqual(ownerName, result.Owner.OwnerName);
            Assert.AreEqual(email, result.Owner.OwnerEmail);
        }
        finally
        {
            await DeleteOwnerAsync(email);
        }
    }

    [TestMethod]
    public async Task CreateOwnerAsync_ReturnsDuplicate_WhenEmailAlreadyExists()
    {
        var email = BuildTestEmail();
        const string password = "SamplePassword123!";
        await InsertOwnerAsync(email, password);

        try
        {
            var service = new SiteOwnerAuthService(ConnectionString);

            var result = await service.CreateOwnerAsync("Duplicate Owner", email, "NewPassword123!");

            Assert.AreEqual(CreateSiteOwnerStatus.EmailAlreadyExists, result.Status);
            Assert.IsNull(result.Owner);
            Assert.AreEqual(1, await CountOwnersByEmailAsync(email));
        }
        finally
        {
            await DeleteOwnerAsync(email);
        }
    }

    [TestMethod]
    public async Task LoadSitesByOwnerAsync_ReturnsOnlySitesForThatOwner_OrderedBySiteName()
    {
        var ownerOneEmail = BuildTestEmail();
        var ownerTwoEmail = BuildTestEmail();
        const string password = "SamplePassword123!";
        var ownerOneId = await InsertOwnerAsync(ownerOneEmail, password);
        var ownerTwoId = await InsertOwnerAsync(ownerTwoEmail, password);
        var siteIds = new List<int>();

        try
        {
            siteIds.Add(await InsertSiteAsync(ownerOneId, "Zulu Plaza"));
            siteIds.Add(await InsertSiteAsync(ownerOneId, "Alpha Center"));
            siteIds.Add(await InsertSiteAsync(ownerTwoId, "Other Owner Site"));

            var service = new SiteOwnerAuthService(ConnectionString);

            var sites = await service.LoadSitesByOwnerAsync(ownerOneId);

            CollectionAssert.AreEqual(new[] { "Alpha Center", "Zulu Plaza" }, sites.Select(site => site.SiteName).ToArray());
            CollectionAssert.AreEqual(siteIds.Take(2).OrderBy(id => id == siteIds[1] ? 0 : 1).ToArray(), sites.Select(site => site.SiteId).ToArray());
        }
        finally
        {
            await DeleteSitesAsync(siteIds);
            await DeleteOwnerAsync(ownerOneEmail);
            await DeleteOwnerAsync(ownerTwoEmail);
        }
    }

    [TestMethod]
    public async Task UpdateSiteAsync_UpdatesOnlyOwnedSiteName()
    {
        var ownerOneEmail = BuildTestEmail();
        var ownerTwoEmail = BuildTestEmail();
        const string password = "SamplePassword123!";
        var ownerOneId = await InsertOwnerAsync(ownerOneEmail, password);
        var ownerTwoId = await InsertOwnerAsync(ownerTwoEmail, password);
        var siteIds = new List<int>();

        try
        {
            var editableSiteId = await InsertSiteAsync(ownerOneId, "Old Name");
            siteIds.Add(editableSiteId);
            siteIds.Add(await InsertSiteAsync(ownerTwoId, "Other Owner Site"));

            var service = new SiteOwnerAuthService(ConnectionString);

            await service.UpdateSiteAsync(editableSiteId, ownerOneId, "Updated Name");
            var sites = await service.LoadSitesByOwnerAsync(ownerOneId);

            CollectionAssert.AreEqual(new[] { "Updated Name" }, sites.Select(site => site.SiteName).ToArray());
        }
        finally
        {
            await DeleteSitesAsync(siteIds);
            await DeleteOwnerAsync(ownerOneEmail);
            await DeleteOwnerAsync(ownerTwoEmail);
        }
    }

    [TestMethod]
    public async Task DeleteSiteAsync_DeletesOnlyOwnedSite()
    {
        var ownerOneEmail = BuildTestEmail();
        var ownerTwoEmail = BuildTestEmail();
        const string password = "SamplePassword123!";
        var ownerOneId = await InsertOwnerAsync(ownerOneEmail, password);
        var ownerTwoId = await InsertOwnerAsync(ownerTwoEmail, password);
        var siteIds = new List<int>();

        try
        {
            var deletableSiteId = await InsertSiteAsync(ownerOneId, "Delete Me");
            siteIds.Add(deletableSiteId);
            var remainingSiteId = await InsertSiteAsync(ownerTwoId, "Keep Me");
            siteIds.Add(remainingSiteId);

            var service = new SiteOwnerAuthService(ConnectionString);

            await service.DeleteSiteAsync(deletableSiteId, ownerOneId);
            var ownerOneSites = await service.LoadSitesByOwnerAsync(ownerOneId);
            var ownerTwoSites = await service.LoadSitesByOwnerAsync(ownerTwoId);

            Assert.IsEmpty(ownerOneSites);
            CollectionAssert.AreEqual(new[] { remainingSiteId }, ownerTwoSites.Select(site => site.SiteId).ToArray());
            CollectionAssert.AreEqual(new[] { "Keep Me" }, ownerTwoSites.Select(site => site.SiteName).ToArray());
        }
        finally
        {
            await DeleteSitesAsync(siteIds);
            await DeleteOwnerAsync(ownerOneEmail);
            await DeleteOwnerAsync(ownerTwoEmail);
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

    private static async Task DeleteSitesAsync(IEnumerable<int> siteIds)
    {
        var ids = siteIds.ToArray();
        if (ids.Length == 0)
        {
            return;
        }

        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM site WHERE site_id IN ({string.Join(", ", ids)});";

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

    private static async Task<int> CountOwnersByEmailAsync(string email)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM site_owner WHERE owner_email = @email;";
        command.Parameters.AddWithValue("@email", email);

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

}
