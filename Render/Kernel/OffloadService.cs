using Render.Interfaces;
using Render.Models.Audio;
using Render.Models.LocalOnlyData;
using Render.Models.Project;
using Render.Models.Scope;
using Render.Models.Sections;
using Render.Repositories.Audio;
using Render.Repositories.Kernel;
using Render.Repositories.LocalDataRepositories;
using Render.TempFromVessel.Project;

namespace Render.Kernel
{
    public class OffloadService : IOffloadService
    {
        private readonly ViewModelContextProvider _viewModelContextProvider;
        private readonly IDataPersistence<Project> _projectRepository;
        private readonly ILocalProjectsRepository _localProjectsRepository;
        private readonly IOffloadAudioRepository _offloadAudioRepository;
        private readonly IMachineLoginStateRepository _machineLoginStateRepository;
        private readonly IDataPersistence<WorkflowStatus> _workflowStatusRepository;
        private readonly IRenderLogger _logger;

        public OffloadService(ViewModelContextProvider viewModelContextProvider)
        {
            _viewModelContextProvider = viewModelContextProvider;
            _projectRepository = _viewModelContextProvider.GetPersistence<Project>();
            _machineLoginStateRepository = viewModelContextProvider.GetMachineLoginStateRepository();
            _workflowStatusRepository = viewModelContextProvider.GetPersistence<WorkflowStatus>();
            _localProjectsRepository = viewModelContextProvider.GetLocalProjectsRepository();
            _offloadAudioRepository = viewModelContextProvider.GetOffloadAudioRepository();
            _logger = viewModelContextProvider.GetLogger(GetType());
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

        public async Task OffloadIncompleteProjects()
        {
            var localProjects = await _localProjectsRepository.GetLocalProjectsForMachine();
            foreach (var localProject in localProjects.GetDownloadedProjects().ToList())
            {
                //Query for workflow status if there are none, we did not successfully download the entire project.
                var workflowStatuses = await _workflowStatusRepository.QueryOnFieldAsync("ProjectId",
                    localProject.ProjectId.ToString(), 0);
            
                if (await _projectRepository.GetAsync(localProject.ProjectId) is null || workflowStatuses.Count == 0)
                {
                    //Deleting all project-related data that managed to get into the bucket
                    await ClearDataForProject(localProject.ProjectId);
                    localProjects.RemoveProjectFromMachine(localProject.ProjectId);
                }
            }

            //as well as delete the local state if there are no completely downloaded projects on devices 
            if (!localProjects.GetDownloadedProjects().Any())
            {
                await _localProjectsRepository.PurgeAllLocalProjects(localProjects.Id);
                var machineState = await _machineLoginStateRepository.GetMachineLoginState();
                await _machineLoginStateRepository.PurgeAllLoginState(machineState.Id);
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
        

        private async Task ClearDataForProject(Guid projectId)
        {
            try
            {
                await _offloadAudioRepository.OffloadAudioForProject(projectId);
                var communityTestRepository = _viewModelContextProvider.GetCommunityTestRepository();
                await communityTestRepository.Purge(projectId);
                var sectionRepository = _viewModelContextProvider.GetSectionRepository();
                await sectionRepository.Purge(projectId);
                var snapshotRepository = _viewModelContextProvider.GetSnapshotRepository();
                await snapshotRepository.Purge(projectId);
                var scopeRepository = _viewModelContextProvider.GetPersistence<Scope>();
                await scopeRepository.PurgeAllOfTypeForProjectId(projectId);
                var statisticsRepository = _viewModelContextProvider.GetPersistence<RenderProjectStatistics>();
                await statisticsRepository.PurgeAllOfTypeForProjectId(projectId);
                var referenceRepository = _viewModelContextProvider.GetPersistence<Reference>();
                await referenceRepository.PurgeAllOfTypeForProjectId(projectId);
                var workflowStatusRepository = _viewModelContextProvider.GetPersistence<WorkflowStatus>();
                await workflowStatusRepository.PurgeAllOfTypeForProjectId(projectId);
                var workflowRepository = _viewModelContextProvider.GetWorkflowRepository();
                await workflowRepository.Purge(projectId);
                var userRepository = _viewModelContextProvider.GetUserRepository();
                //Only removes the Render Users for the project, not any global Users
                await userRepository.Purge(projectId);
                await _projectRepository.PurgeAllOfTypeForProjectId(projectId);
                var renderProjectRepository = _viewModelContextProvider.GetPersistence<RenderProject>();
                await renderProjectRepository.PurgeAllOfTypeForProjectId(projectId);
                var sessionStateService = _viewModelContextProvider.GetSessionStateService();
                await sessionStateService.ResetSessionAsync();
                var localProjects = await _localProjectsRepository.GetLocalProjectsForMachine();
                localProjects.SetNotStartedState(projectId);
                await _localProjectsRepository.SaveLocalProjectsForMachine(localProjects);
            }
            catch (Exception e)
            {
                _logger.LogError(e);
            }
        }

        public async Task<string> GetOffloadProjectSize(Guid projectId)
        {
            try
            {
                var audioIntegrityService = _viewModelContextProvider.GetAudioIntegrityService();
                var projectTotalSize = await audioIntegrityService.CalculateProjectSize(projectId);
				return GetFormattedProjectSize(projectTotalSize);
            }
            catch (Exception e)
            {
                _logger.LogError(e);
                return string.Empty;
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