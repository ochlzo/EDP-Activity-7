namespace edp_gui_app;

public sealed partial class MainAppWindow
{
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _documentRequestServer.Dispose();
        base.OnFormClosed(e);
    }
}
