using Render.Models.LocalOnlyData;
using Render.Repositories.Kernel;

namespace Render.Repositories.LocalDataRepositories
{
    public class UserMachineSettingsRepository : IUserMachineSettingsRepository
    {
        private readonly IDataPersistence<UserMachineSettings> _userMachineSettingsPersistence;

        public UserMachineSettingsRepository(IDataPersistence<UserMachineSettings> userMachineSettingsPersistence)
        {
            _userMachineSettingsPersistence = userMachineSettingsPersistence;
        }
        public async Task<UserMachineSettings> GetUserMachineSettingsForUserAsync(Guid userId)
        {
            var userMachineSettings = await _userMachineSettingsPersistence.QueryOnFieldAsync("UserId", userId.ToString());
            return userMachineSettings ?? new UserMachineSettings(userId);
        }

        public async Task UpdateUserMachineSettingsAsync(UserMachineSettings userMachineSettings)
        {
            await _userMachineSettingsPersistence.UpsertAsync(userMachineSettings.Id, userMachineSettings);
        }
        
        public void Dispose()
        {
            _userMachineSettingsPersistence.Dispose();
        }
    }
}