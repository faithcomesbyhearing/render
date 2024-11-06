using Render.Models.Users;
using Render.TempFromVessel.Project;

namespace Render.Repositories.UserRepositories
{
    public interface IUserRepository : IDisposable
    {
        Task<List<IUser>> GetUsersForProjectAsync(Project project);

        Task SaveUserAsync(IUser user);

        Task DeleteUserAsync(IUser user);

        Task<IUser> GetUserAsync(Guid userId);

        Task<List<IUser>> GetAllUsersAsync();

        Task<IUser> GetUserAsync(string username);
    }
}