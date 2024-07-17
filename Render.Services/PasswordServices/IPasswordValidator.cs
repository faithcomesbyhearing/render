using Microsoft.AspNetCore.Identity;
using Render.Models.Users;

namespace Render.Services.PasswordServices
{
    public interface IPasswordValidator
    {
        PasswordVerificationResult ValidatePassword(IUser user, string password);

        string HashPassword(IUser user, string password)
        {
            return String.Empty;
        }
    }
}