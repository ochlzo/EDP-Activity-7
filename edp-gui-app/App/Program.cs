namespace edp_gui_app;

internal static class Program
{
    private const string ConnectionString =
        "Server=127.0.0.1;Port=3306;Database=site_management;User ID=root;Password=NewStrongPassword123!;";

    [STAThread]
    private static void Main()
    {
        LocalEnvironmentLoader.LoadFromCurrentDirectory();
        ApplicationConfiguration.Initialize();
        Application.Run(new MainAppWindow(
            new SiteOwnerAuthService(ConnectionString),
            new OwnerMaintenanceService(ConnectionString)));
    }
}
