using edp_gui_app;
using MySqlConnector;

namespace edp_gui_app.Tests;

[TestClass]
public sealed class SiteOwnerProfileServiceTests
{
    private const string ConnectionString =
        "Server=127.0.0.1;Port=3306;Database=site_management;User ID=root;Password=NewStrongPassword123!;SslMode=None;";

    [TestMethod]
    public async Task UpdateOwnerDetailsAsync_UpdatesNameAndContactNumber()
    {
        var email = BuildTestEmail();
        const string password = "SamplePassword123!";
        var ownerId = await InsertOwnerAsync(email, password);

        try
        {
            var service = new SiteOwnerAuthService(ConnectionString);

            await service.UpdateOwnerDetailsAsync(ownerId, "Updated Owner", "09171234567");

            var profile = await service.LoadOwnerProfileAsync(ownerId);
            Assert.AreEqual("Updated Owner", profile.OwnerName);
            Assert.AreEqual("09171234567", profile.ContactNumber);
            Assert.AreEqual(email, profile.OwnerEmail);
        }
        finally
        {
            await DeleteOwnerAsync(email);
        }
    }

    [TestMethod]
    public async Task UpdateOwnerEmailAsync_RequiresCurrentPasswordAndUpdatesImmediately()
    {
        var email = BuildTestEmail();
        var newEmail = BuildTestEmail();
        const string password = "SamplePassword123!";
        var ownerId = await InsertOwnerAsync(email, password);

        try
        {
            var service = new SiteOwnerAuthService(ConnectionString);

            await service.UpdateOwnerEmailAsync(ownerId, password, newEmail);

            var updatedProfile = await service.LoadOwnerProfileAsync(ownerId);
            Assert.AreEqual(newEmail, updatedProfile.OwnerEmail);
            Assert.IsNull(await service.AuthenticateAsync(email, password));
            Assert.IsNotNull(await service.AuthenticateAsync(newEmail, password));
        }
        finally
        {
            await DeleteOwnerAsync(email);
            await DeleteOwnerAsync(newEmail);
        }
    }

    [TestMethod]
    public async Task UpdateOwnerEmailAsync_ThrowsWhenEmailAlreadyExists()
    {
        var currentEmail = BuildTestEmail();
        var duplicateEmail = BuildTestEmail();
        const string password = "SamplePassword123!";
        var ownerId = await InsertOwnerAsync(currentEmail, password);
        await InsertOwnerAsync(duplicateEmail, password);

        try
        {
            var service = new SiteOwnerAuthService(ConnectionString);

            var threw = false;
            try
            {
                await service.UpdateOwnerEmailAsync(ownerId, password, duplicateEmail);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }

            Assert.IsTrue(threw);

            Assert.IsNotNull(await service.AuthenticateAsync(currentEmail, password));
            Assert.IsNotNull(await service.AuthenticateAsync(duplicateEmail, password));
            Assert.AreEqual(currentEmail, (await service.LoadOwnerProfileAsync(ownerId)).OwnerEmail);
        }
        finally
        {
            await DeleteOwnerAsync(currentEmail);
            await DeleteOwnerAsync(duplicateEmail);
        }
    }

    [TestMethod]
    public async Task UpdateOwnerPasswordAsync_ChangesPasswordAfterReentry()
    {
        var email = BuildTestEmail();
        const string currentPassword = "SamplePassword123!";
        const string newPassword = "NewPassword123!";
        var ownerId = await InsertOwnerAsync(email, currentPassword);

        try
        {
            var service = new SiteOwnerAuthService(ConnectionString);

            await service.UpdateOwnerPasswordAsync(ownerId, currentPassword, newPassword);

            Assert.IsNull(await service.AuthenticateAsync(email, currentPassword));
            Assert.IsNotNull(await service.AuthenticateAsync(email, newPassword));
        }
        finally
        {
            await DeleteOwnerAsync(email);
        }
    }

    [TestMethod]
    public async Task UpdateOwnerPasswordAsync_ThrowsWhenCurrentPasswordIsIncorrect()
    {
        var email = BuildTestEmail();
        const string password = "SamplePassword123!";
        var ownerId = await InsertOwnerAsync(email, password);

        try
        {
            var service = new SiteOwnerAuthService(ConnectionString);

            var threw = false;
            try
            {
                await service.UpdateOwnerPasswordAsync(ownerId, "WrongPassword123!", "NewPassword123!");
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }

            Assert.IsTrue(threw);

            Assert.IsNotNull(await service.AuthenticateAsync(email, password));
        }
        finally
        {
            await DeleteOwnerAsync(email);
        }
    }

    private static string BuildTestEmail() => $"codex_profile_{Guid.NewGuid():N}@example.com";

    private static async Task<int> InsertOwnerAsync(string email, string password)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO site_owner (owner_name, owner_email, password)
            VALUES (@name, @email, @password);
            """;
        command.Parameters.AddWithValue("@name", "Codex Profile Owner");
        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@password", password);

        await command.ExecuteNonQueryAsync();

        return Convert.ToInt32(command.LastInsertedId);
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
