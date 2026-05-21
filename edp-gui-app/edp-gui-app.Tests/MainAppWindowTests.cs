using edp_gui_app;
using System.Windows.Forms;

namespace edp_gui_app.Tests;

[TestClass]
public sealed class MainAppWindowTests
{
    [TestMethod]
    public void MainAppWindow_CanBeConstructed()
    {
        using var window = new MainAppWindow(new SiteOwnerAuthService("Server=127.0.0.1;Database=test;"));

        Assert.IsNotNull(window);
    }

    [TestMethod]
    public void ResetPasswordStage_HidesEmailAndCodeInputs_WhenCodeIsVerified()
    {
        using var window = new MainAppWindow(new SiteOwnerAuthService("Server=127.0.0.1;Database=test;"));

        window.Show();
        Invoke(window, "ShowResetPasswordView", "owner@example.com");
        Invoke(window, "SetResetPasswordStage", true);

        Assert.IsFalse(GetControl<TextBox>(window, "_resetPasswordEmailTextBox").Visible);
        Assert.IsFalse(GetControl<TextBox>(window, "_resetPasswordCodeTextBox").Visible);
        Assert.IsTrue(GetControl<TextBox>(window, "_resetPasswordNewPasswordTextBox").Visible);
        Assert.IsTrue(GetControl<TextBox>(window, "_resetPasswordConfirmPasswordTextBox").Visible);
    }

    [TestMethod]
    public void TenantDetailsDialog_ShowsReplaceTenantButton()
    {
        using var window = new MainAppWindow(new SiteOwnerAuthService("Server=127.0.0.1;Database=test;"));
        var method = typeof(MainAppWindow).GetMethod(
            "BuildTenantDetailsDialog",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        var dialog = (Form)method.Invoke(
            window,
            [
                new OwnedTenant(1, "Tenant", "tenant@example.com", "Address", "09170000000"),
                Array.Empty<OwnedDocumentAttachment>(),
                new Func<Task<bool>>(() => Task.FromResult(false))
            ])!;

        using (dialog)
        {
            Assert.IsNotNull(FindButton(dialog, "Replace Tenant"));
        }
    }

    private static void Invoke(MainAppWindow window, string methodName, params object?[] args)
    {
        var method = typeof(MainAppWindow).GetMethod(
            methodName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        method.Invoke(window, args);
    }

    private static T GetControl<T>(MainAppWindow window, string fieldName)
        where T : Control
    {
        var field = typeof(MainAppWindow).GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        return (T)field.GetValue(window)!;
    }

    private static Button? FindButton(Control root, string text)
    {
        foreach (Control child in root.Controls)
        {
            if (child is Button button && button.Text == text)
            {
                return button;
            }

            var nested = FindButton(child, text);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }
}
