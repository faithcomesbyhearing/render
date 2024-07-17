using Render.Models.Users;
using Render.TempFromVessel.AdministrativeGroups;
using Render.TempFromVessel.Project;

namespace Render.Services.UserServices
{
    public interface IUserMembershipService : IDisposable
    {
        Task<List<AdministrativeGroup>> GetExplicitAdministrativeGroupAssignmentsForUserAsync(Guid userId);

        Task<IList<Project>> GetExplicitProjectAssignmentsForUserAsync(Guid userId);

        Task<IList<Guid>> GetExplicitAdministratorsForProjectAsync(Guid projectId);

        Task<bool> HasInheritedPermission(User user, AdministrativeGroup group);

        Task<bool> HasInheritedPermissionForProject(User user, Project project);

        Task<List<Guid>> GetAllPermittedProjectIdsAsync(Guid userId);

        bool HasExplicitPermissionForProject(IUser user, Guid projectId);
    }
}