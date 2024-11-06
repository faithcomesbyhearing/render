using Render.Models.LocalOnlyData;

namespace Render.Repositories.LocalDataRepositories
{
    public interface ISessionStateRepository
    {
        Task<List<UserProjectSession>> GetUserProjectSessionAsync(Guid userId, Guid projectId);

        Task SaveSessionStateAsync(UserProjectSession session);
    }
}