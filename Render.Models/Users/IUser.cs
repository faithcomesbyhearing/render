using Render.TempFromVessel.User;

namespace Render.Models.Users
{
    public enum UserType
    {
        Render,
        Vessel
    }

    public interface IUser
    {
        /// <summary>
        /// The full name of the person; enough that a project or group administrator can
        /// identify the person within the context of the software application.
        /// </summary>
        string FullName { get; set; }

        Guid Id { get; }

        /// <summary>
        /// The username is unique across the entire system, and is used for logging a user in.
        /// </summary>
        string Username { get; set; }

        byte[] UserIcon { get; }

        string HashedPassword { get; set; }

        bool IsGridPassword { get; set; }

        List<Guid> RoleIds { get; set; }

        List<VesselClaim> Claims { get; set; }

        UserType UserType { get; }

        string SyncGatewayLogin { get; set; }
        
        string LocaleId { get; set; }

        string Environment { get; }

        bool HasClaim(string claimType, string claimName = null, Guid roleId = default);
        
        bool HasRole(Role role);

        bool HasRole(Guid roleId);

        List<Guid> RoleIdsForProject(string claimType, string claimName);

        List<RoleName> RolesForProject(string claimType, string claimName);
    }
}