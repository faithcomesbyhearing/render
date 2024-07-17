using Microsoft.AspNetCore.Identity;
using Render.Models.Users;

namespace Render.Services.PasswordServices
{
    public class RenderTextPasswordValidator : IPasswordValidator
    {
        public PasswordVerificationResult ValidatePassword(IUser user, string password)
        {
            return user.HashedPassword == password ? 
                PasswordVerificationResult.Success : 
                PasswordVerificationResult.Failed;
        }
    }
}