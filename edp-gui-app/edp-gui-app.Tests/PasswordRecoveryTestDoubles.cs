using edp_gui_app;

namespace edp_gui_app.Tests;

internal sealed class CapturingEmailSender : IEmailSender
{
    public List<PasswordResetEmailMessage> Messages { get; } = [];
    public List<DocumentRequestEmailMessage> DocumentRequestMessages { get; } = [];

    public Task SendPasswordResetAsync(
        PasswordResetEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }

    public Task SendDocumentRequestAsync(
        DocumentRequestEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        DocumentRequestMessages.Add(message);
        return Task.CompletedTask;
    }
}

internal sealed class FixedPasswordResetCodeGenerator(string code) : IPasswordResetCodeGenerator
{
    public string CreateCode() => code;
}

internal sealed class TestClock(DateTime utcNow) : IClock
{
    public DateTime UtcNow { get; set; } = utcNow;
}
