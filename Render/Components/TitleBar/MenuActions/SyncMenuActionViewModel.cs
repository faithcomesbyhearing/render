using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Project;
using Render.Repositories.Kernel;
using Render.Repositories.LocalDataRepositories;
using Render.Resources;
using Render.Resources.Localization;
using Render.Services.SyncService;
using Render.TempFromVessel.Project;

namespace Render.Components.TitleBar.MenuActions
{
    public class SyncMenuActionViewModel : ViewModelBase
    {
        private IDataPersistence<Project> _projectRepository;
        private ILocalProjectsRepository _localProjectsRepository;
        private ISyncService _syncService;
        private ILocalSyncService _localSyncService;

        public bool IsWebSync { get; private set; }

        [Reactive]
        public CurrentSyncStatus CurrentSyncStatus { get; private set; }

        public bool IsManualSync { get; set; }

        public string Title { get; set; }

        public readonly ReactiveCommand<Unit, Unit> Command;

        public string Glyph { get; set; }
        private string PageName;

        public SyncMenuActionViewModel(IViewModelContextProvider viewModelContextProvider, string pageName) : base("SyncMenuAction", viewModelContextProvider)
        {
            _projectRepository = viewModelContextProvider.GetPersistence<Project>();
            _localProjectsRepository = viewModelContextProvider.GetLocalProjectsRepository();
            _syncService = viewModelContextProvider.GetSyncService();
            _localSyncService = viewModelContextProvider.GetLocalSyncService();
            Glyph = ((FontImageSource)ResourceExtensions.GetResourceValue("SyncIcon"))?.Glyph;
            Title = AppResources.Sync;
            PageName = pageName;
            Command = ReactiveCommand.CreateFromTask(SyncNowAsync);
            if (_syncService.CurrentSyncStatus == CurrentSyncStatus.ActiveReplication)
            {
                IsWebSync = true;
            }
            else if (_localSyncService.CurrentSyncStatus == CurrentSyncStatus.ActiveReplication)
            {
                IsWebSync = false;
            }
            Disposables.Add(this.WhenAnyValue(x => x._syncService.CurrentSyncStatus)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(status =>
                {
                    if (IsWebSync)
                    {
                        CurrentSyncStatus = status;
                    }
                }));
            Disposables.Add(this.WhenAnyValue(x => x._localSyncService.CurrentSyncStatus)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(status =>
                {
                    if (!IsWebSync)
                    {
                        CurrentSyncStatus = status;
                    }
                }));
        }

        private async Task SyncNowAsync()
        {
#if DEMO
            return;
#endif
            var currentUser = ViewModelContextProvider.GetLoggedInUser();
            if (currentUser is null)
            {
                return;
            }

            IsManualSync = true;
            var localProjects = await _localProjectsRepository.GetLocalProjectsForMachine();

            var projectIdList = new List<Guid>();
            var globalUserIds = new List<Guid>();

            foreach (var project in localProjects.GetDownloadedProjects())
            {
                var originProject = await _projectRepository.GetAsync(project.ProjectId);

                if (originProject != null)
                {
                    globalUserIds.AddRange(originProject.GlobalUserIds);
                }
                projectIdList.Add(project.ProjectId);
            }

            if (await ViewModelContextProvider.GetSyncGatewayApiWrapper().IsConnected())
            {
                IsWebSync = true;
                //Need to call start all sync here if we didn't do it on login because we were lan connected
                _syncService.StartAllSync(projectIdList, globalUserIds, currentUser.Id.ToString(), currentUser.SyncGatewayLogin);
                foreach (var project in localProjects.GetDownloadedProjects())
                {
                    project.LastSyncDate = DateTime.Now;

                    //Update Project Statistics
                    var statisticsPersistence = ViewModelContextProvider.GetPersistence<RenderProjectStatistics>();
                    var projectStatistics = (await statisticsPersistence.QueryOnFieldAsync("ProjectId", project.ProjectId.ToString(), 1, false)).FirstOrDefault();
                    if (projectStatistics != null &&
                        (DateTimeOffset.Now - projectStatistics.RenderProjectLastSyncDate) > TimeSpan.FromMinutes(1))
                    {
                        projectStatistics.SetRenderProjectLastSyncDate(DateTimeOffset.Now);
                        await statisticsPersistence.UpsertAsync(projectStatistics.Id, projectStatistics);
                    }
                }

                await _localProjectsRepository.SaveLocalProjectsForMachine(localProjects);
            }
            else
            {
                IsWebSync = false;
                _localSyncService.StartLocalSync(currentUser.Username, GetProjectId());
                CloseMenu();
            }

            LogInfo("Button Clicked", new Dictionary<string, string>
            {
                {"Button Name", "Sync Now"},
                {"ViewModel", nameof(SyncMenuActionViewModel)},
                {"Project ID(s)", string.Join(", ", projectIdList)},
                {"User ID(s)", string.Join(", ", globalUserIds)},
                {"LoggedInUserId", GetLoggedInUserId().ToString()},
                {"ProjectId", GetProjectId().ToString()}
            });
        }

        protected void CloseMenu() => ViewModelContextProvider.GetMenuPopupService().Close();

        public override void Dispose()
        {
            _projectRepository?.Dispose();
            _localProjectsRepository?.Dispose();
            Command?.Dispose();
            base.Dispose();
        }
    }
}
