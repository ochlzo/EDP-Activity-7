using edp_gui_app;

namespace edp_gui_app.Tests;

[TestClass]
public sealed class LocalEnvironmentLoaderTests
{
    [TestMethod]
    public void Load_SetsVariablesFromEnvFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.env");
        File.WriteAllText(path, """
            # Gmail password reset settings
            GOOGLE_OAUTH_CLIENT_ID=local-client
            GOOGLE_OAUTH_CLIENT_SECRET="local secret"
            GOOGLE_OAUTH_REFRESH_TOKEN='local-refresh'
            GMAIL_SENDER_EMAIL=sender@example.com
            """);

        try
        {
            Clear("GOOGLE_OAUTH_CLIENT_ID");
            Clear("GOOGLE_OAUTH_CLIENT_SECRET");
            Clear("GOOGLE_OAUTH_REFRESH_TOKEN");
            Clear("GMAIL_SENDER_EMAIL");

            LocalEnvironmentLoader.Load(path);

            Assert.AreEqual("local-client", Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_ID"));
            Assert.AreEqual("local secret", Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_SECRET"));
            Assert.AreEqual("local-refresh", Environment.GetEnvironmentVariable("GOOGLE_OAUTH_REFRESH_TOKEN"));
            Assert.AreEqual("sender@example.com", Environment.GetEnvironmentVariable("GMAIL_SENDER_EMAIL"));
        }
        finally
        {
            File.Delete(path);
            Clear("GOOGLE_OAUTH_CLIENT_ID");
            Clear("GOOGLE_OAUTH_CLIENT_SECRET");
            Clear("GOOGLE_OAUTH_REFRESH_TOKEN");
            Clear("GMAIL_SENDER_EMAIL");
        }
    }

    [TestMethod]
    public void Load_DoesNotOverwriteExistingProcessVariable()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.env");
        File.WriteAllText(path, "GMAIL_SENDER_EMAIL=file@example.com");

        try
        {
            Environment.SetEnvironmentVariable("GMAIL_SENDER_EMAIL", "process@example.com");

            LocalEnvironmentLoader.Load(path);

            Assert.AreEqual("process@example.com", Environment.GetEnvironmentVariable("GMAIL_SENDER_EMAIL"));
        }
        finally
        {
            File.Delete(path);
            Clear("GMAIL_SENDER_EMAIL");
        }
    }

    private static void Clear(string name) => Environment.SetEnvironmentVariable(name, null);
}
