using Microsoft.AspNetCore.Identity;
using Render.Models.Users;

namespace Render.Services.PasswordServices
{
    public class VesselPasswordValidator : IPasswordValidator
    {
        private readonly PasswordHasher<User> _hasher;

        public VesselPasswordValidator()
        {
            _hasher = new PasswordHasher<User>();
        }
        
        public PasswordVerificationResult ValidatePassword(IUser user, string password)
        {
            return _hasher.VerifyHashedPassword((User)user, user.HashedPassword, password);
        }

        public string HashPassword(IUser user, string password)
        {
            return _hasher.HashPassword((User)user, password);
        }
    }
}