namespace edp_gui_admin;

public sealed partial class AdminRecordService
{
    private async Task LogAdminActivityAsync(
        string action,
        string entityType,
        int entityId,
        string description,
        CancellationToken cancellationToken)
    {
        var transactionService = new AdminTransactionService(_connectionString);
        await transactionService.LogActivityAsync(
            "Admin",
            "admin",
            action,
            entityType,
            entityId,
            description,
            cancellationToken);
    }
}
