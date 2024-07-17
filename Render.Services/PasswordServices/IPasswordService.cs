namespace Render.Services.PasswordServices;

public interface IPasswordService
{
    /// <summary>
    /// Generates a password that conforms to our criteria
    /// </summary>
    /// <returns>The password.</returns>
    string GeneratePassword();
}