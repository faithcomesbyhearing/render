using Render.Models.Users;
using Render.TempFromVessel.User;

namespace Render.Services.UserServices
{
    /// <summary>
    /// Service for managing User Membership
    /// </summary>
    /// <seealso cref="IUserMembershipService" />
    public class UserMembershipService : IUserMembershipService
    {
        public bool HasExplicitPermissionForProject(IUser user, Guid projectId)
        {
            return user is User &&
                   user.HasClaim(RenderRolesAndClaims.ProjectUserClaimType, projectId.ToString());
        }
    }
}