using Render.Interfaces;
using Render.Models.LocalOnlyData;
using Render.Repositories.Kernel;
using Render.Repositories.LocalDataRepositories;
using Render.Services.SessionStateServices;
using Render.Services.SyncService;
using Render.TempFromVessel.Project;

namespace Render.Kernel
{
    public class OffloadService : IOffloadService
    {
        private readonly IDataPersistence<Project> _projectRepository;
        private readonly ILocalProjectsRepository _localProjectsRepository;
        private readonly IOffloadRepository _offloadRepository;
        private readonly ISessionStateService _sessionStateService;
        private readonly IAudioIntegrityService _audioIntegrityService;
        private readonly IRenderLogger _logger;

        public OffloadService(
            IDataPersistence<Project> projectRepository,
            ILocalProjectsRepository localProjectsRepository,
            IOffloadRepository offloadRepository,
            ISessionStateService sessionStateService, 
            IAudioIntegrityService audioIntegrityService,
            IRenderLogger logger)
        {
            _projectRepository = projectRepository;
            _localProjectsRepository = localProjectsRepository;
            _offloadRepository = offloadRepository;
            _sessionStateService = sessionStateService;
            _audioIntegrityService = audioIntegrityService;
            _logger = logger;
        }

        public async Task OffloadProject(Guid projectId)
        {
            var project = await _projectRepository.GetAsync(projectId);
            if (project != null)
            {
                await ClearDataForProject(projectId);
                await _projectRepository.UpsertAsync(projectId, project);
            }
        }
        
        public async Task OffloadProjectsData(List<Guid> projectsIds)
        {
            foreach (var projectId in projectsIds)
            {
                await ClearDataForProject(projectId);  
            }
            
            var localProjects = await _localProjectsRepository.GetLocalProjectsForMachine();
            
            //delete the local state if there are no downloaded projects on devices 
            if (!localProjects.GetDownloadedProjects().Any())
            {
                await _offloadRepository.PurgeRenderLocalOnlyData();
            }
        }
        
        public async Task OffloadFailedProjects()
        {
            var localProjects = await _localProjectsRepository.GetLocalProjectsForMachine();
            var projects = localProjects.GetProjects();
            var failedProjects = projects
                .Where(project => 
                    project.State == DownloadState.Downloading || 
                    project.State == DownloadState.Offloading || 
                    project.State == DownloadState.Canceling)
                .Select(x => x.ProjectId);
            
            foreach (var projectId in failedProjects)
            {
                await OffloadProject(projectId);
            }
        }
        
        public async Task<string> GetOffloadProjectSize(Guid projectId)
        {
            try
            {
                var projectTotalSize = await _audioIntegrityService.CalculateProjectSize(projectId);
                return GetFormattedProjectSize(projectTotalSize);
            }
            catch (Exception e)
            {
                _logger.LogError(e);
                return string.Empty;
            }
        }
        
        private async Task ClearDataForProject(Guid projectId)
        {
            try
            {
                await _offloadRepository.OffloadAsync(projectId);
                await _sessionStateService.ResetSessionAsync();
                var localProjects = await _localProjectsRepository.GetLocalProjectsForMachine();
                localProjects.SetNotStartedState(projectId);
                await _localProjectsRepository.SaveLocalProjectsForMachine(localProjects);
            }
            catch (Exception e)
            {
                _logger.LogError(e);
            }
        }
        
        private string GetFormattedProjectSize(int projectTotalSize)
        {
            var totalSizeLength = projectTotalSize.ToString().Length;

            string measure;

            switch (totalSizeLength)
            {
                case int n when n <= 3:
                    measure = $"{projectTotalSize} bytes";
                    break;

                case int n when n <= 6 && n > 3:
                    measure = $"{Math.Round((double)projectTotalSize / 1024)} KB";
                    break;
                case int n when n <= 9 && n > 6:
                    measure = $"{Math.Round((double)projectTotalSize / 1024 / 1024)} MB";
                    break;

                default:
                    measure = $"{Math.Round((double)projectTotalSize / 1024 / 1024 / 1024)} GB";
                    break;
            }

            return measure;
        }
    }
}