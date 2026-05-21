using System.Security.Cryptography;

namespace edp_gui_app;

public interface IPasswordResetCodeGenerator
{
    string CreateCode();
}

public sealed class PasswordResetCodeGenerator : IPasswordResetCodeGenerator
{
    public string CreateCode()
    {
        return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
    }
}
