using Render.Models.Users;
using Render.Repositories.Kernel;
using Render.TempFromVessel.Project;
using Render.TempFromVessel.User;

namespace Render.Repositories.UserRepositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDataPersistence<User> _userPersistence;
        private readonly IDataPersistence<RenderUser> _renderUserPersistence;

        public UserRepository(
            IDataPersistence<User> userPersistence,
            IDataPersistence<RenderUser> renderUserPersistence)
        {
            _userPersistence = userPersistence;
            _renderUserPersistence = renderUserPersistence;
        }
        
        public async Task<List<IUser>> GetUsersForProjectAsync(Project project)
        {
            var renderUsers = await _renderUserPersistence.QueryOnFieldAsync("ProjectId", project.Id.ToString(), 0);
            var vesselUsers = await _userPersistence.GetAllOfTypeAsync();
            var filteredVesselUsers = new List<User>(); 

            foreach (var user in vesselUsers)
            {
                if (user.HasClaim(VesselRolesAndClaims.ProjectUserClaimType, project.Id.ToString())
                    || user.HasClaim(VesselRolesAndClaims.AdministrativeGroupUserClaimType, project.ParentGroupId.ToString()))
                {
                    filteredVesselUsers.Add(user);
                }
            }

            var users = new List<IUser>();
            users.AddRange(renderUsers);
            users.AddRange(filteredVesselUsers);

            return users;
        }

        public async Task SaveUserAsync(IUser user)
        {
            if (user.UserType == UserType.Render)
            {
                await _renderUserPersistence.UpsertAsync(user.Id, (RenderUser)user);
            }
            else
            {
                await _userPersistence.UpsertAsync(user.Id, (User)user);
            }
        }

        public async Task DeleteUserAsync(IUser user)
        {
            if (user.UserType == UserType.Render)
            {
                await _renderUserPersistence.DeleteAsync(user.Id);
            }
            else
            {
                await _userPersistence.DeleteAsync(user.Id);
            }
        }

        public async Task<IUser> GetUserAsync(Guid userId)
        {
            var renderUserResult = await _renderUserPersistence.GetAsync(userId);
            if (renderUserResult != null)
            {
                return renderUserResult;
            }

            var vesselUser = await _userPersistence.GetAsync(userId);
            return vesselUser;
        }

        public async Task<IUser> GetUserAsync(string username)
        {
            var renderUserResult = await _renderUserPersistence.QueryOnFieldAsync("Username", username);
            if (renderUserResult != null)
            {
                return renderUserResult;
            }

            var vesselUser = await _userPersistence.QueryOnFieldAsync("Username", username);
            return vesselUser;
        }

        public async Task<List<IUser>> GetAllUsersAsync()
        {
            var renderUsers = await _renderUserPersistence.GetAllOfTypeAsync();
            var vesselUsers = await _userPersistence.GetAllOfTypeAsync();
            var result = new List<IUser>(renderUsers);
            result.AddRange(vesselUsers);
            return result;
        }
        
        public async Task Purge(Guid id)
        { 
            await _renderUserPersistence.PurgeAllOfTypeForProjectId(id);
        }

        public void Dispose()
        {
            _userPersistence?.Dispose();
            _renderUserPersistence?.Dispose();
        }
    }
}