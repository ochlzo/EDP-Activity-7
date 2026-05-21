namespace edp_gui_admin;

internal static class Program
{
    private const string ConnectionString =
        "Server=127.0.0.1;Port=3306;Database=site_management;User ID=root;Password=NewStrongPassword123!;";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new AdminAppWindow(
            new AdminAuthService(ConnectionString),
            new AdminRecordService(ConnectionString),
            new AdminTransactionService(ConnectionString)));
    }
}
