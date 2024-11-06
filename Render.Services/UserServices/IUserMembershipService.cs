using Render.Models.Users;

namespace Render.Services.UserServices
{
    public interface IUserMembershipService
    {
        bool HasExplicitPermissionForProject(IUser user, Guid projectId);
    }
}