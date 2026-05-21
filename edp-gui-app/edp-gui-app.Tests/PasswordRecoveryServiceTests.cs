using edp_gui_app;
using MySqlConnector;

namespace edp_gui_app.Tests;

[TestClass]
public sealed class PasswordRecoveryServiceTests
{
    private const string ConnectionString =
        "Server=127.0.0.1;Port=3306;Database=site_management;User ID=root;Password=NewStrongPassword123!;SslMode=None;";

    [TestMethod]
    public async Task RequestPasswordResetAsync_EmailsCode_WhenOwnerExists()
    {
        var email = BuildTestEmail();
        await InsertOwnerAsync(email, "OldPassword123!");
        var sender = new CapturingEmailSender();
        var service = CreateService(sender, "123456");

        try
        {
            var status = await service.RequestPasswordResetAsync(email);

            Assert.AreEqual(PasswordResetRequestStatus.SentIfAccountExists, status);
            Assert.HasCount(1, sender.Messages);
            Assert.AreEqual(email, sender.Messages[0].ToEmail);
            Assert.AreEqual("123456", sender.Messages[0].Code);
        }
        finally
        {
            await DeleteOwnerAsync(email);
        }
    }

    [TestMethod]
    public async Task RequestPasswordResetAsync_ReturnsEmailDoesNotExist_WhenOwnerIsMissing()
    {
        var sender = new CapturingEmailSender();
        var service = CreateService(sender, "123456");

        var status = await service.RequestPasswordResetAsync(BuildTestEmail());

        Assert.AreEqual(PasswordResetRequestStatus.EmailDoesNotExist, status);
        Assert.IsEmpty(sender.Messages);
    }

    [TestMethod]
    public async Task ResetPasswordAsync_UpdatesPassword_WhenCodeMatches()
    {
        var email = BuildTestEmail();
        await InsertOwnerAsync(email, "OldPassword123!");
        var service = CreateService(new CapturingEmailSender(), "654321");

        try
        {
            await service.RequestPasswordResetAsync(email);

            var status = await service.ResetPasswordAsync(email, "654321", "NewPassword123!");
            var owner = await service.AuthenticateAsync(email, "NewPassword123!");
            var oldOwner = await service.AuthenticateAsync(email, "OldPassword123!");

            Assert.AreEqual(PasswordResetStatus.Reset, status);
            Assert.IsNotNull(owner);
            Assert.IsNull(oldOwner);
        }
        finally
        {
            await DeleteOwnerAsync(email);
        }
    }

    [TestMethod]
    public async Task VerifyPasswordResetCodeAsync_ReturnsValid_WhenCodeMatches()
    {
        var email = BuildTestEmail();
        await InsertOwnerAsync(email, "OldPassword123!");
        var service = CreateService(new CapturingEmailSender(), "333444");

        try
        {
            await service.RequestPasswordResetAsync(email);

            var status = await service.VerifyPasswordResetCodeAsync(email, "333444");
            var owner = await service.AuthenticateAsync(email, "OldPassword123!");

            Assert.AreEqual(PasswordResetCodeStatus.Valid, status);
            Assert.IsNotNull(owner);
        }
        finally
        {
            await DeleteOwnerAsync(email);
        }
    }

    [TestMethod]
    public async Task ResetPasswordAsync_RejectsExpiredCode()
    {
        var email = BuildTestEmail();
        await InsertOwnerAsync(email, "OldPassword123!");
        var clock = new TestClock(new DateTime(2026, 5, 8, 0, 0, 0, DateTimeKind.Utc));
        var service = CreateService(new CapturingEmailSender(), "111222", clock);

        try
        {
            await service.RequestPasswordResetAsync(email);
            clock.UtcNow = clock.UtcNow.AddMinutes(16);

            var status = await service.ResetPasswordAsync(email, "111222", "NewPassword123!");

            Assert.AreEqual(PasswordResetStatus.InvalidOrExpired, status);
            Assert.IsNull(await service.AuthenticateAsync(email, "NewPassword123!"));
        }
        finally
        {
            await DeleteOwnerAsync(email);
        }
    }

    private static SiteOwnerAuthService CreateService(
        IEmailSender sender,
        string code,
        IClock? clock = null)
    {
        return new SiteOwnerAuthService(
            ConnectionString,
            sender,
            new FixedPasswordResetCodeGenerator(code),
            clock ?? new TestClock(DateTime.UtcNow));
    }

    private static string BuildTestEmail() => $"codex_reset_{Guid.NewGuid():N}@example.com";

    private static async Task<int> InsertOwnerAsync(string email, string password)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO site_owner (owner_name, owner_email, password)
            VALUES (@name, @email, @password);
            """;
        command.Parameters.AddWithValue("@name", "Codex Reset Owner");
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
