using Render.Models.Users;

namespace Render.Services.PasswordServices
{
    public class PasswordValidatorFactory
    {
        public IPasswordValidator GetValidator(IUser user)
        {
            switch (user.UserType)
            {
                case UserType.Render when user.IsGridPassword:
                    return new RenderGridPasswordValidator();
                case UserType.Render when !user.IsGridPassword:
                    return new RenderTextPasswordValidator();
                case UserType.Vessel:
                    return new VesselPasswordValidator();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}