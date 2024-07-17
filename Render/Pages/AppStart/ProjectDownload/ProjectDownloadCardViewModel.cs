using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.LocalOnlyData;
using Render.Models.Project;
using Render.Models.Users;
using Render.Repositories.LocalDataRepositories;
using Render.Resources;
using Render.Resources.Localization;
using Render.TempFromVessel.Project;

namespace Render.Pages.AppStart.ProjectDownload
{
    public class ProjectDownloadCardViewModel : ViewModelBase
    {
        private readonly ILocalProjectsRepository _localProjectsRepository;
        private readonly Action<LocalProject> _refreshProjectSelectCardViewList;
        private readonly IOffloadService _offloadService;
        private readonly IProjectDownloadService _projectDownloadService;
        private readonly IAudioLossRetryDownloadService _audioLossRetryDownloadService;
        private LocalProject _localProject;
        private readonly IUser _user;
        
        [Reactive] public Project Project { get; private set; }
        [Reactive] public DownloadState DownloadState { get; private set; }

        public ReactiveCommand<Unit, Unit> DownloadProjectCommand { get; }
        
        public ReactiveCommandBase<Unit, Unit> RetryDownloadProjectCommand { get; }

        public ReactiveCommand<Unit, Unit> CancelProjectCommand { get; }

        public ProjectDownloadCardViewModel(Project project, IViewModelContextProvider viewModelContextProvider,
            DownloadState downloadState, Action<LocalProject> refreshProjectSelectCardViewList = null) :
            base("ProjectDownloadCard", viewModelContextProvider)
        {
            Project = project;
            _localProjectsRepository = viewModelContextProvider.GetLocalProjectsRepository();
            _projectDownloadService = viewModelContextProvider.GetProjectDownloaderService();
            _offloadService = viewModelContextProvider.GetOffloadService();
            _user = ViewModelContextProvider.GetLoggedInUser();
            _audioLossRetryDownloadService = viewModelContextProvider.GetAudioLossRetryDownloadService();
            
            DownloadState = downloadState;
            if (downloadState is DownloadState.Downloading)
            {
                SubscribeToProjectDownloadFinished(project.Id);
            }

            DownloadProjectCommand = ReactiveCommand.CreateFromTask(StartDownload,
                this.WhenAnyValue(x => x.DownloadState).Select(s => s == DownloadState.NotStarted));

            RetryDownloadProjectCommand = ReactiveCommand.CreateFromTask(RetryDownload,
                this.WhenAnyValue(x => x.DownloadState).Select(s => s == DownloadState.FinishedPartially));

            CancelProjectCommand = ReactiveCommand.Create(Cancel);
            
            _refreshProjectSelectCardViewList = refreshProjectSelectCardViewList;

            Disposables.Add(DownloadProjectCommand.ThrownExceptions.Subscribe(async exception =>
            {
                var errorMessage = string.Format(AppResources.ErrorWithMessage, exception.Message);
                await viewModelContextProvider.GetModalService()
                    .ShowInfoModal(Icon.DownloadError, errorMessage, AppResources.ContactSupport);
            }));
        }

        private async Task StartDownload()
        {
            if (!await ViewModelContextProvider.GetSyncGatewayApiWrapper().IsConnected())
            {
                return;
            }

            DownloadState = DownloadState.Downloading;
            
            _projectDownloadService.StartDownload(Project.Id, Project.GlobalUserIds,
                syncGatewayUsername: _user.Id.ToString(), syncGatewayPassword: _user.SyncGatewayLogin);
            SubscribeToProjectDownloadFinished(Project.Id);
            
            await DownloadProjectsToMachineAsync();
        }
        
        private async Task RetryDownload()
        {
            if (!await ViewModelContextProvider.GetSyncGatewayApiWrapper().IsConnected())
            {
                return;
            }

            DownloadState = DownloadState.Downloading;
            
            _projectDownloadService.ResumeDownload(Project.Id, Project.GlobalUserIds,
                syncGatewayUsername: _user.Id.ToString(), syncGatewayPassword: _user.SyncGatewayLogin);
            SubscribeToProjectDownloadFinished(Project.Id);
            
            await DownloadProjectsToMachineAsync();
        }

        private void Cancel()
        {
            DownloadState = DownloadState.Canceling;
            _projectDownloadService.StopDownload(Project.Id);
            Task.Run(async () =>
            {
                await SetCancelStateForProjectAndOffload();
            });
        }

        private void SubscribeToProjectDownloadFinished(Guid projectId)
        {
            Disposables.Add(_projectDownloadService.WhenDownloadFinished(projectId)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async completed =>
                {
                    if (DownloadState == DownloadState.Downloading)
                    {
                        if (completed)
                        {
                            var downloadResult = await _audioLossRetryDownloadService.RetryDownloadIfAudioLoss(StartDownload, projectId, 1);
                            if (downloadResult.AudioIsBroken && downloadResult.AutomaticRetryCompleted is false)
                            {
                                return;   
                            }
                        }
                        
                        DownloadState = completed ? DownloadState.Finished : DownloadState.FinishedPartially;

                        await AddProjectsToMachineAsync(DownloadState);

                        if (_localProject != null)
                        {
                            _localProject.State = DownloadState;
                            _refreshProjectSelectCardViewList?.Invoke(_localProject);
                        }
                    }
                    else if (!completed && DownloadState == DownloadState.Canceling)
                    {
                        await _offloadService.OffloadProject(Project.Id);
                    }
                }));
        }

        private async Task DownloadProjectsToMachineAsync()
        {
            var localProject = await _localProjectsRepository.GetLocalProjectsForMachine();
            localProject.Download(Project.Id);
            var project = localProject.GetProject(Project.Id);
            if (project != null)
            {
                project.LastSyncDate = DateTime.Now;
            }
            await _localProjectsRepository.SaveLocalProjectsForMachine(localProject);
            _localProject = project;

            //Update Project Statistics
            var statisticsPersistence = ViewModelContextProvider.GetPersistence<RenderProjectStatistics>();
            var projectStatistics =
                (await statisticsPersistence.QueryOnFieldAsync("ProjectId", Project.Id.ToString(), 1, false))
                .FirstOrDefault();
            if (projectStatistics != null)
            {
                projectStatistics.SetRenderProjectLastSyncDate(DateTimeOffset.Now);
                await statisticsPersistence.UpsertAsync(projectStatistics.Id, projectStatistics);
            }
        }

        private async Task SetCancelStateForProjectAndOffload()
        {
            var localProject = await _localProjectsRepository.GetLocalProjectsForMachine();
            localProject.Cancel(Project.Id);
            await _localProjectsRepository.SaveLocalProjectsForMachine(localProject);

            await _offloadService.OffloadProject(Project.Id);

            DownloadState = DownloadState.NotStarted;
        }

        private async Task AddProjectsToMachineAsync(DownloadState downloadState)
        {
            var localProject = await _localProjectsRepository.GetLocalProjectsForMachine();

            if (downloadState is DownloadState.Finished)
            {
                localProject.AddDownloadedProject(Project.Id);
            }
            else
            {
                localProject.AddPartiallyDownloadedProject(Project.Id);
            }
            
            await _localProjectsRepository.SaveLocalProjectsForMachine(localProject);
        }
    }
}