using Render.Models.LocalOnlyData;

namespace Render.Repositories.LocalDataRepositories
{
    public interface ILocalProjectsRepository : IDisposable
    {
        Task<LocalProjects> GetLocalProjectsForMachine();

        Task SaveLocalProject(Guid projectId, bool projectDownloadedCompletely);
        
        Task SaveLocalProjectsForMachine(LocalProjects localProjects);

        Task SaveUpdates(LocalProjects localProjects);
    }
}