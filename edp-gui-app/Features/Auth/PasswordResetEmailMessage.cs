namespace edp_gui_app;

public sealed record PasswordResetEmailMessage(string ToEmail, string Code);
