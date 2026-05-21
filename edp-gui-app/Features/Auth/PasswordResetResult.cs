namespace edp_gui_app;

public enum PasswordResetRequestStatus
{
    SentIfAccountExists,
    EmailNotConfigured,
    EmailDoesNotExist
}

public enum PasswordResetStatus
{
    Reset,
    InvalidOrExpired
}

public enum PasswordResetCodeStatus
{
    Valid,
    InvalidOrExpired
}
