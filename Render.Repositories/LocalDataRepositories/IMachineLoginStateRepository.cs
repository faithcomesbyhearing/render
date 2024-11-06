using Render.Models.LocalOnlyData;

namespace Render.Repositories.LocalDataRepositories
{
    public interface IMachineLoginStateRepository : IDisposable
    {
        Task<MachineLoginState> GetMachineLoginState();

        Task SaveMachineLoginState(MachineLoginState machineLoginState);
    }
}