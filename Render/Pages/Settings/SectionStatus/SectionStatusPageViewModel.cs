using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.TitleBar.MenuActions;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Audio;
using Render.Models.Project;
using Render.Models.Scope;
using Render.Models.Sections;
using Render.Models.Snapshot;
using Render.Pages.AppStart.Home;
using Render.Pages.Settings.SectionStatus.Processes;
using Render.Pages.Settings.SectionStatus.Recovery;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;
using Render.Services.FileNaming;
using Render.Services.AudioServices;
using Render.TempFromVessel.Project;
using Render.TempFromVessel.User;
using System.IO.Compression;

namespace Render.Pages.Settings.SectionStatus
{
    public enum ExportingStatus
    {
        Exporting,
        Completed,
        None
    }

    public class SectionStatusPageViewModel : PageViewModelBase
    {
        private bool _navigateBackShouldNavigateToHome;
        private Dictionary<string, IEnumerable<Audio>> _audiosToExport = [];

        private readonly IFileNameGeneratorService _fileNameGenerator;
        private readonly IDownloadService _downloadService;
        private readonly Guid _projectId;

        public SectionStatusProcessesViewModel ProcessesViewModel { get; private set; }

        public SectionStatusRecoveryViewModel RecoveryViewModel { get; private set; }

        public ReactiveCommand<Unit, Unit> SelectProcessesViewCommand { get; private set; }

        public ReactiveCommand<Unit, Unit> SelectRecoveryViewCommand { get; private set; }

        public ReactiveCommand<Unit, Unit> ExportCommand { get; private set; }

        [Reactive]
        public bool ShowProcessesView { get; set; }

        [Reactive]
        public bool IsConfigure { get; set; }

        [Reactive]
        public bool ConflictPresent { get; set; }

        [Reactive]
        public bool EnableExportButton { get; set; }

        [Reactive]
        public double ExportPercent { get; set; }

        [Reactive]
        public string ExportedString { get; set; }

        [Reactive]
        public ExportingStatus Status { get; private set; } = ExportingStatus.None;

        public static async Task<SectionStatusPageViewModel> CreateAsync(
            IViewModelContextProvider viewModelContextProvider,
            Guid projectId)
        {
            var sectionRepository = viewModelContextProvider.GetSectionRepository();
            var allSections = await sectionRepository.GetSectionsForProjectAsync(projectId);

            //get all scopes by project id
            var scopePersistence = viewModelContextProvider.GetPersistence<Scope>();
            var projectScopes = await scopePersistence.QueryOnFieldAsync("ProjectId", projectId.ToString(), 0);

            // Only show sections whose scope is active.
            var activeSections = allSections.Where(section => projectScopes.Any(x => x.Status == "Active" && x.Id == section.ScopeId)).ToList();

            var recoveryViewModel = await SectionStatusRecoveryViewModel.CreateAsync(activeSections, viewModelContextProvider);
            var processesViewModel = await SectionStatusProcessesViewModel.CreateAsync(activeSections, viewModelContextProvider);
            var vm = new SectionStatusPageViewModel(projectId, viewModelContextProvider, processesViewModel, recoveryViewModel, default);
            return vm;
        }

        private SectionStatusPageViewModel(
            Guid projectId,
            IViewModelContextProvider viewModelContextProvider,
            SectionStatusProcessesViewModel processesViewModel,
            SectionStatusRecoveryViewModel sectionStatusRecoveryViewModel,
            List<IMenuActionViewModel> menuActionViewModels = null,
            int sectionNumber = 0,
            Audio sectionTitleAudio = null)
            : base(
                  "SectionStatusPage",
                  viewModelContextProvider,
                  AppResources.ProjectHome,
                  menuActionViewModels,
                  sectionNumber,
                  sectionTitleAudio,
                  secondPageName: AppResources.SectionStatus)
        {
            _fileNameGenerator = ViewModelContextProvider.GetFileNameGeneratorService();
            _downloadService = ViewModelContextProvider.GetDownloadService();
            _projectId = projectId;

            ProcessesViewModel = processesViewModel;

            RecoveryViewModel = sectionStatusRecoveryViewModel;
            RecoveryViewModel.SectionRecovered += SectionRecovered;

            ShowProcessesView = true;
            ProcessesViewModel.ShowProcessView = true;
            var user = viewModelContextProvider.GetLoggedInUser();
            if (user.HasClaim(RenderRolesAndClaims.ProjectUserClaimType,
                    _projectId.ToString(),
                    RoleName.Configure.GetRoleId()))
            {
                IsConfigure = true;
            }
            var color = (ColorReference)ResourceExtensions.GetResourceValue("SecondaryText");
            if (color != null)
            {
                TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(Icon.Home, color.Color, 35)?.Glyph;
            }
            SelectProcessesViewCommand = ReactiveCommand.Create(SelectProcessesView);
            SelectRecoveryViewCommand = ReactiveCommand.Create(SelectRecoveryView);

            var canExecuteExportAsync = this.WhenAnyValue(x => x.EnableExportButton);
            ExportCommand = ReactiveCommand.CreateFromTask(ExportAsync, canExecuteExportAsync);

            TitleBarViewModel.PageGlyph = ((FontImageSource)ResourceExtensions.GetResourceValue("SectionStatusIcon"))?.Glyph;
            TitleBarViewModel.NavigateBackCommand = ReactiveCommand.CreateFromTask(NavigateBack);

            Disposables.Add(this.WhenAnyValue(x => x.RecoveryViewModel.ConflictPresent)
                .Subscribe(conflictPresent =>
                {
                    ConflictPresent = conflictPresent;
                }));

            Disposables.Add(TitleBarViewModel.TitleBarMenuViewModel.NavigationItems.Observable
                .ObserveOn(RxApp.MainThreadScheduler)
                .MergeMany(item => item.Command.IsExecuting)
                .Subscribe(SetLoadingScreen));

            Disposables.Add(TitleBarViewModel.NavigationItems.Observable
                .MergeMany(item => item.IsExecuting)
                .Subscribe(isExecuting => IsLoading = isExecuting));

            Disposables.Add(this
                .WhenAnyValue(
                    x => x.ProcessesViewModel.IsLoading,
                    x => x.RecoveryViewModel.IsLoading)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(tuple =>
                {
                    var (processesIsLoading, recoveryIsLoading) = tuple;

                    IsLoading = processesIsLoading || recoveryIsLoading;
                }));

            Disposables.Add(ProcessesViewModel
                .WhenAnyValue(p => p.AnySectionSelectedToExport)
                .Subscribe(anySectionSelectedToExport =>
                {
                    EnableExportButton = anySectionSelectedToExport;
                }));

            Disposables.Add(this.WhenAnyValue(p => p.Status)
                .Subscribe(s => HideProgressBar()));

            Disposables.Add(ExportCommand.ThrownExceptions
                .Subscribe(async exception =>
                {
                    var result = await viewModelContextProvider
                    .GetModalService()
                    .ConfirmationModal(
                        Icon.TypeWarning,
                        AppResources.Error,
                        AppResources.DownloadFailed,
                        AppResources.Cancel,
                        AppResources.TryAgain);

                    if (result == DialogResult.Ok)
                    {
                        await ExportAsync();
                    }
                    else
                    {
                        Status = ExportingStatus.None;
                    }
                }));
        }

        private async void SectionRecovered()
        {
            var grandCentralStation = ViewModelContextProvider.GetGrandCentralStation();
            var loggedInUserId = ViewModelContextProvider.GetLoggedInUser().Id;
            await grandCentralStation.FindWorkForUser(_projectId, loggedInUserId);
            var sectionRepository = ViewModelContextProvider.GetSectionRepository();
            var allSections = await sectionRepository.GetSectionsForProjectAsync(_projectId);

            await ProcessesViewModel.InitializeStageCard(allSections, ViewModelContextProvider);
        }

        private void SelectRecoveryView()
        {
            ShowProcessesView = false;
            ProcessesViewModel.ShowProcessView = false;
            RecoveryViewModel.ShowRecoveryView = true;
            _navigateBackShouldNavigateToHome = true;
            Pause();
        }

        private void SelectProcessesView()
        {
            ShowProcessesView = true;
            ProcessesViewModel.ShowProcessView = true;
            RecoveryViewModel.ShowRecoveryView = false;
            Pause();
        }

        public void Pause()
        {
            ViewModelContextProvider.GetAudioActivityService().Stop();
        }

        private new async Task<IRoutableViewModel> NavigateBack()
        {
            if (HostScreen.Router.NavigationStack.IsPreviousScreenHome() || _navigateBackShouldNavigateToHome)
            {
                var home = await HomeViewModel.CreateAsync(_projectId, ViewModelContextProvider);
                return await NavigateToAndReset(home);
            }

            return await base.NavigateBack();
        }

        private async Task ExportAsync()
        {
            ExportPercent = 0.0;
            var totalToExport = ProcessesViewModel.SectionToExportList.Count();
            ExportedString = string.Format(AppResources.Exporting, 0, totalToExport);
            var renderProjectRepository = ViewModelContextProvider.GetPersistence<RenderProject>();
            var projectRepository = ViewModelContextProvider.GetPersistence<Project>();
            var renderProject = await renderProjectRepository.QueryOnFieldAsync("ProjectId", _projectId.ToString());
            var scopeRepository = ViewModelContextProvider.GetPersistence<Scope>();
            var permission = await ViewModelContextProvider.GetEssentials().AskForFileAccessPermissions();

            if (permission)
            {
                var numberExported = 0.0;
                ExportPercent = 0.0;
                var chosenDirectory = (await _downloadService.ChooseFolderAsync())?.Path;
                if (chosenDirectory != null)
                {
                    Status = ExportingStatus.Exporting;
                    EnableExportButton = false;

                    var sectionRepository = ViewModelContextProvider.GetSectionRepository();
                    var snapshotService = ViewModelContextProvider.GetSnapshotService();

                    foreach (var sectionToExport in ProcessesViewModel.SectionToExportList)
                    {
                        _audiosToExport.Clear();

                        string stageFrom = sectionToExport.StageFrom is not null
                            ? sectionToExport.StageFrom.Name
                            : sectionToExport.Section.ApprovedBy != Guid.Empty
                                ? AppResources.Approved
                                : AppResources.Unassigned;

                        var project = await projectRepository.QueryOnFieldAsync("Id", _projectId.ToString());
                        var projectName = project.Name;
                        var scope = await scopeRepository.QueryOnFieldAsync("Id", sectionToExport.Section.ScopeId.ToString());
                        var scopeName = scope.Name;
                        var autonim = renderProject.GetLanguageName();

                        if (sectionToExport.HasConflict)
                        {
                            var snapshotsWithAudio = new List<Snapshot>();
                            var conflictedSnapshots = await snapshotService.GetLastConflictedSnapshots(sectionToExport.Section.Id);

                            snapshotsWithAudio = conflictedSnapshots
                                .Select(conflict => conflict.Snapshot)
                                .ToList();

                            for (int i = 0; i < snapshotsWithAudio.Count(); i++)
                            {
                                GenerateAudiosToExport(
                                    passages: snapshotsWithAudio[i].Passages,
                                    sectionToExport.Section,
                                    stageFrom,
                                    autonim,
                                    sectionToExport.HasConflict,
                                    index: i);
                            }
                        }
                        else
                        {
                            var sectionWithAudio = await sectionRepository.GetSectionWithDraftsAsync(
                                sectionToExport.Section.Id,
                                getRetellBackTranslations: true,
                                getSegmentBackTranslations: true);

                            GenerateAudiosToExport(
                                sectionWithAudio.Passages,
                                sectionWithAudio,
                                stageFrom,
                                autonim,
                                hasConflict: false,
                                index: default);
                        }

                        var zipArchiveName = _fileNameGenerator.GetFileNameForScopeAudiosZip(sectionToExport.Section, projectName, scopeName);
                        var zipPath = Path.Combine(chosenDirectory, zipArchiveName);
                        await ZipAudios(zipPath);

                        numberExported++;
                        ExportPercent = numberExported / totalToExport;
                        ExportedString = string.Format(AppResources.Exporting, numberExported, totalToExport);
                    }
                }

                Status = ExportingStatus.Completed;
                EnableExportButton = true;
            }
        }

        private void HideProgressBar()
        {
            if (Status == ExportingStatus.Completed)
            {
                Status = ExportingStatus.None;
            }
        }

        private void GenerateAudiosToExport(
            List<Passage> passages,
            Section section,
            string stageName,
            string autonim,
            bool hasConflict,
            int index)
        {
            var audioGroups = passages.GetAudioGroups();

            foreach (var audioGroup in audioGroups)
            {
                var name = _fileNameGenerator.GetFileNameForAudioGroup(
                    section,
                    stageName,
                    autonim,
                    hasConflict,
                    index,
                    audioGroup);

                _audiosToExport.Add(name, audioGroup.Audios);
            }
        }

        private async Task ZipAudios(string zipPath)
        {
            await using var filestream = new FileStream(zipPath, FileMode.OpenOrCreate);
            using var zipArchive = new ZipArchive(filestream, ZipArchiveMode.Update);
            foreach (var audio in _audiosToExport)
            {
                var playback = new AudioPlayback(Guid.NewGuid(), audio.Value);
                var tempAudioService = ViewModelContextProvider.GetTempAudioService(playback);

                await using var sequenceAudioStream = tempAudioService.OpenAudioStream();
                ZipArchiveEntry entry = zipArchive.GetEntry(audio.Key);
                entry?.Delete();
                entry = zipArchive.CreateEntry(audio.Key);

                await using var entryStream = entry.Open();
                await sequenceAudioStream.CopyToAsync(entryStream);
            }
        }

        public override void Dispose()
        {
            ProcessesViewModel?.Dispose();
            ProcessesViewModel = null;

            RecoveryViewModel?.Dispose();
            RecoveryViewModel = null;

            SelectRecoveryViewCommand?.Dispose();
            SelectRecoveryViewCommand = null;
            SelectProcessesViewCommand?.Dispose();
            SelectProcessesViewCommand = null;
            ExportCommand?.Dispose();
            ExportCommand = null;

            _audiosToExport.Clear();

            base.Dispose();
        }
    }
}