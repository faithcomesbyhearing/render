using Render.Models.LocalOnlyData;
using Render.Repositories.Kernel;

namespace Render.Repositories.LocalDataRepositories
{
    public class MachineLoginStateRepository : IMachineLoginStateRepository
    {
        private readonly IDataPersistence<MachineLoginState> _machineLoginStatePersistence;

        public MachineLoginStateRepository(IDataPersistence<MachineLoginState> machineLoginStatePersistence)
        {
            _machineLoginStatePersistence = machineLoginStatePersistence;
        }

        public async Task<MachineLoginState> GetMachineLoginState()
        {
            var all = await _machineLoginStatePersistence.GetAllOfTypeAsync();
            return all.FirstOrDefault() ?? new MachineLoginState(); 
        }

        public async Task SaveMachineLoginState(MachineLoginState machineLoginState)
        {
            await _machineLoginStatePersistence.UpsertAsync(machineLoginState.Id, machineLoginState);
        }
        
        public async Task PurgeForProject(Guid projectId)
        { 
            await _machineLoginStatePersistence.PurgeAllOfTypeForProjectId(projectId);
        }
        
        public async Task PurgeAllLoginState(Guid machineLoginStateEntityId)
        {
            await _machineLoginStatePersistence.PurgeAllOfTypeForId(machineLoginStateEntityId);
        }

        public void Dispose()
        {
            _machineLoginStatePersistence.Dispose();
        }
    }
}