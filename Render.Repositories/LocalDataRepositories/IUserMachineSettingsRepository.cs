using Render.Models.LocalOnlyData;

namespace Render.Repositories.LocalDataRepositories
{
    public interface IUserMachineSettingsRepository : IDisposable
    {
        Task<UserMachineSettings> GetUserMachineSettingsForUserAsync(Guid userId);

        Task UpdateUserMachineSettingsAsync(UserMachineSettings userMachineSettings);

        Task Purge(Guid id);
    }
}