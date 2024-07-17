using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.TitleBar.MenuActions;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Audio;
using Render.Models.Scope;
using Render.Pages.AppStart.Home;
using Render.Pages.Settings.SectionStatus.Processes;
using Render.Pages.Settings.SectionStatus.Recovery;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;
using Render.TempFromVessel.User;

namespace Render.Pages.Settings.SectionStatus
{
    public class SectionStatusPageViewModel : PageViewModelBase
    {
        private readonly Guid _projectId;
        private bool _navigateBackShouldNavigateToHome;

        public SectionStatusProcessesViewModel ProcessesViewModel { get; private set; }
        public SectionStatusRecoveryViewModel RecoveryViewModel { get; private set; }

        public ReactiveCommand<Unit, Unit> SelectProcessesViewCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> SelectRecoveryViewCommand { get; private set; }

        [Reactive]
        public bool ShowProcessesView { get; set; }

        [Reactive]
        public bool IsConfigure { get; set; }
        [Reactive]
        public bool ConflictPresent { get; set; }

        public static async Task<SectionStatusPageViewModel> CreateAsync(IViewModelContextProvider viewModelContextProvider,
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

        private SectionStatusPageViewModel(Guid projectId, IViewModelContextProvider viewModelContextProvider,
            SectionStatusProcessesViewModel processesViewModel,
            SectionStatusRecoveryViewModel sectionStatusRecoveryViewModel, List<IMenuActionViewModel> menuActionViewModels = null, 
            int sectionNumber = 0, Audio sectionTitleAudio = null) : base("SectionStatusPage", viewModelContextProvider, AppResources.ProjectHome,
            menuActionViewModels, sectionNumber, sectionTitleAudio, secondPageName: AppResources.SectionStatus)
        {
            DisposeOnNavigationCleared = true;
            TitleBarViewModel.DisposeOnNavigationCleared = true;

            _projectId = projectId;

            ProcessesViewModel = processesViewModel;

            RecoveryViewModel = sectionStatusRecoveryViewModel;
            RecoveryViewModel.SectionRecovered += SectionRecovered;

            ShowProcessesView = true;
            ProcessesViewModel.ShowProcessView = true;
            var user = viewModelContextProvider.GetLoggedInUser();
            if (user.HasClaim(RenderRolesAndClaims.ProjectUserClaimType,
                    _projectId.ToString(),
                    RenderRolesAndClaims.GetRoleByName(RoleName.Configure).Id))
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

            Disposables.Add(this.WhenAnyValue(
                    x => x.ProcessesViewModel.IsLoading,
                    x => x.RecoveryViewModel.IsLoading
                )
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(tuple =>
                {
                    var (processesIsLoading, recoveryIsLoading) = tuple;
            
                    IsLoading = processesIsLoading || recoveryIsLoading;
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

            base.Dispose();
        }
    }
}