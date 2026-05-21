namespace edp_gui_app;

public interface IEmailSender
{
    Task SendPasswordResetAsync(
        PasswordResetEmailMessage message,
        CancellationToken cancellationToken = default);

    Task SendDocumentRequestAsync(
        DocumentRequestEmailMessage message,
        CancellationToken cancellationToken = default);
}
