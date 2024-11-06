using Render.Models.LocalOnlyData;
using Render.Repositories.Kernel;

namespace Render.Repositories.LocalDataRepositories
{
    public class LocalProjectsRepository : ILocalProjectsRepository
    {
        private readonly IDataPersistence<LocalProjects> _localProjectsDataAccess;
        
        public LocalProjectsRepository(IDataPersistence<LocalProjects> localProjectsDataAccess)
        {
            _localProjectsDataAccess = localProjectsDataAccess;
        }
        
        public async Task<LocalProjects> GetLocalProjectsForMachine()
        {
            var allLocalProjects = await _localProjectsDataAccess.GetAllOfTypeAsync();
            return allLocalProjects.FirstOrDefault() ?? new LocalProjects(); 
        }

        public async Task SaveLocalProject(Guid projectId, bool projectDownloadedCompletely)
        {
            var localProject = await GetLocalProjectsForMachine();

            if (projectDownloadedCompletely)
            {
                localProject.AddDownloadedProject(projectId);
            }
            else
            {
                localProject.AddPartiallyDownloadedProject(projectId);
            }

            await SaveLocalProjectsForMachine(localProject);
        }

        public async Task SaveLocalProjectsForMachine(LocalProjects localProjects)
        {
            if (localProjects.GetProjectIds().Count > 0)
            {
                await _localProjectsDataAccess.UpsertAsync(localProjects.Id, localProjects);
            }
        }
        
        public async Task SaveUpdates(LocalProjects localProjects)
        {
            await _localProjectsDataAccess.UpsertAsync(localProjects.Id, localProjects);
        }
        
        public void Dispose()
        {
            _localProjectsDataAccess.Dispose();
        }
    }
}