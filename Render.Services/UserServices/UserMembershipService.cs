using Render.Models.Users;
using Render.Repositories.Kernel;
using Render.Repositories.UserRepositories;
using Render.TempFromVessel.AdministrativeGroups;
using Render.TempFromVessel.Project;
using Render.TempFromVessel.User;

namespace Render.Services.UserServices
{
    /// <summary>
    /// Service for managing User Membership
    /// </summary>
    /// <seealso cref="IUserMembershipService" />
    public class UserMembershipService : IUserMembershipService
    {
        private IDataPersistence<AdministrativeGroup> AdministrativeGroupRepository { get; }
		private IDataPersistence<Project> ProjectRepository { get; }
        private IDataPersistence<User> UserRepository { get; }
        private IUserRepository RenderUserRepository { get; }
        private List<Guid> _permittedAdministrativeGroupIdList;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserMembershipService"/> class.
        /// </summary>
        public UserMembershipService(
            IDataPersistence<User> userRepository = null,
            IDataPersistence<AdministrativeGroup> administrativeGroupRepository = null,
            IDataPersistence<Project> projectRepository = null,
            IUserRepository renderUserRepository = null)
        {
            UserRepository = userRepository;
            AdministrativeGroupRepository = administrativeGroupRepository;
            ProjectRepository = projectRepository;
            RenderUserRepository = renderUserRepository;
        }

        /// <summary>
		/// Builds a list of all administrative groups the user is an administrator of.
		/// </summary>
		/// <param name="userId">The user id.</param>
		/// <returns>A list of administrative groups.</returns>
        public async Task<List<AdministrativeGroup>> GetExplicitAdministrativeGroupAssignmentsForUserAsync(Guid userId)
        {
            var allGroups = await AdministrativeGroupRepository.GetAllOfTypeAsync(waitForIndex:true);
            var user = await UserRepository.GetAsync(userId);
	        var adminGroups = new List<AdministrativeGroup>();

            foreach (var group in allGroups)
            {
	            if (user.HasClaim( VesselRolesAndClaims.AdministrativeGroupUserClaimType, group.Id.ToString()))
	            {
					adminGroups.Add(group);
	            }
            }

            return adminGroups;
        }

        /// <summary>
        /// Gets all projects for user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>List of Projects for all login types</returns>
        public async Task<IList<Project>> GetExplicitProjectAssignmentsForUserAsync(Guid userId)
        {
            var allProjects = (await ProjectRepository.GetAllOfTypeAsync()).Where(x => !x.IsDeleted).ToList();
	        var projects = new List<Project>();
            var user = await UserRepository.GetAsync(userId);

	        foreach (var project in allProjects)
	        {
				if(user.HasClaim(VesselRolesAndClaims.ProjectUserClaimType, project.Id.ToString()))
                {
                    projects.Add(project);
                }
			}
	        return projects;
        }

        public async Task<bool> HasInheritedPermission(User user, AdministrativeGroup group)
        {
            bool isAdministrator = false;
            while (!isAdministrator)
            {
                if (user.HasClaim(VesselRolesAndClaims.AdministrativeGroupUserClaimType, group.Id.ToString()))
                {
                    isAdministrator = true;
                    break;
                }
                if (group.ParentId == Guid.Empty)
                {
                    break;
                }
                group = await AdministrativeGroupRepository.GetAsync(group.ParentId);
            }
            if (!isAdministrator)
            {
                return false;
            }
            return isAdministrator;
        }

        public bool HasExplicitPermissionForProject(IUser user, Guid projectId)
        {
            return 
                user is User &&
                user.HasClaim(RenderRolesAndClaims.ProjectUserClaimType, projectId.ToString()) ||
                user.HasClaim(VesselRolesAndClaims.ProjectPolicyName, projectId.ToString()) ||
                user.HasClaim(VesselRolesAndClaims.ProjectUserClaimType, projectId.ToString());
        }

        public async Task<bool> HasInheritedPermissionForProject(User user, Project project)
        {
            var firstGroup = await AdministrativeGroupRepository.GetAsync(project.ParentGroupId);
            if (firstGroup == null)
            {
                return false;
            }

            return await HasInheritedPermission(user, firstGroup);
        }


        public async Task<IList<Guid>> GetExplicitAdministratorsForProjectAsync(Guid projectId)
        {
            var allUsers = await UserRepository.GetAllOfTypeAsync();
            var userList = new List<Guid>();

            foreach(var user in allUsers)
            {
                if(user.HasClaim(VesselRolesAndClaims.ProjectUserClaimType, projectId.ToString(), VesselRolesAndClaims.GetRoleByName(RoleName.ProjectAdministrator).Id))
                {
                    userList.Add(user.Id);
                }
            }
            return userList;
        }
        
        public async Task<List<Guid>> GetAllPermittedProjectIdsAsync(Guid userId)
        {
            var allProjects = (await ProjectRepository.GetAllOfTypeAsync()).Where(x => !x.IsDeleted).ToList();
            var assignedAdministrativeGroups = await GetExplicitAdministrativeGroupAssignmentsForUserAsync(userId);
            var assignedProjects = await GetExplicitProjectAssignmentsForUserAsync(userId);
            var permittedProjectIdList = new List<Guid>();
            _permittedAdministrativeGroupIdList = new List<Guid>();

            permittedProjectIdList.AddRange(assignedProjects.Select(project => project.Id));

            foreach (var assignedGroup in assignedAdministrativeGroups)
            {
                if (!_permittedAdministrativeGroupIdList.Contains(assignedGroup.Id))
                {
                    _permittedAdministrativeGroupIdList.Add(assignedGroup.Id);
                }

                await AddChildGroupIdToPermittedListRecursively(assignedGroup);
            }

            foreach (var permittedGroupId in _permittedAdministrativeGroupIdList)
            {
                foreach(var project in allProjects)
                {
                    if (project.ParentGroupId == permittedGroupId)
                    {
                        if (!permittedProjectIdList.Contains(project.Id))
                        {
                            permittedProjectIdList.Add(project.Id);
                        }
                    }
                }
            }

            return permittedProjectIdList;
        }
        
        private async Task AddChildGroupIdToPermittedListRecursively(AdministrativeGroup group)
        {
            var allGroups = await AdministrativeGroupRepository.GetAllOfTypeAsync(waitForIndex:true);
            var childGroups = allGroups.Where(b => b.ParentId == group.Id).ToList();

            if (childGroups.Count == 0)
            {
                return;
            }

            foreach (var childGroup in childGroups)
            {
                if (!_permittedAdministrativeGroupIdList.Contains(childGroup.Id))
                {
                    _permittedAdministrativeGroupIdList.Add(childGroup.Id);
                }
                await AddChildGroupIdToPermittedListRecursively(childGroup);
            }
        }
        
        public void Dispose()
        {
            AdministrativeGroupRepository?.Dispose();
            ProjectRepository?.Dispose();
            UserRepository?.Dispose();
            RenderUserRepository?.Dispose();
            _permittedAdministrativeGroupIdList = null;
        }
    }
}