using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.AddProject;
using Render.Components.ValidationEntry;
using Render.Kernel;
using Render.Models.LocalOnlyData;
using Render.Repositories.LocalDataRepositories;
using Render.Resources.Localization;

namespace Render.Pages.AppStart.Login
{
    public class AddProjectViaIdViewModel : PageViewModelBase, IAddProjectViewModel, IAddProjectViaIdViewModel
    {
        private bool _allowProjectOffload = true;
        private readonly ILocalProjectsRepository _localProjectsRepository;
        private readonly IOffloadService _offloadService;
        private readonly AddProjectViaIdLocalViewModel _addProjectViaIdLocalViewModel;
        private readonly AddProjectViaIdWebViewModel _addProjectViaIdWebViewModel;
        
        public ValidationEntryViewModel ProjectIdValidationEntry { get; }
        [Reactive] public Guid ProjectId { get; private set; }
        [Reactive] public AddProjectState AddProjectState { get; private set; }
        public ReactiveCommand<Unit, Unit> CancelOnDownloadingCommand { get; }
        public ReactiveCommand<Unit, Unit> RetryOnErrorCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelOnErrorCommand { get; }
        public ReactiveCommand<Unit, IRoutableViewModel> NavigateOnCompletedCommand { get; }
        public string NavigateOnCompletedButtonText { get; } = AppResources.ReturnToLogIn;
        [Reactive] public string DownloadErrorText { get; private set; }
        public ReactiveCommand<Unit, Unit> DownloadProjectCommand { get; }
        [Reactive] public bool AllowDownloadProjectCommand { get; private set; }
        
        
        public AddProjectViaIdViewModel(IViewModelContextProvider viewModelContextProvider) :
            base("AddProjectId", viewModelContextProvider, "Add Project ID")
        {
            _localProjectsRepository = viewModelContextProvider.GetLocalProjectsRepository();
            _offloadService = viewModelContextProvider.GetOffloadService();
            var audioLossRetryDownloadService = viewModelContextProvider.GetAudioLossRetryDownloadService();
            _addProjectViaIdLocalViewModel = new AddProjectViaIdLocalViewModel(viewModelContextProvider, audioLossRetryDownloadService);
            _addProjectViaIdWebViewModel = new AddProjectViaIdWebViewModel(viewModelContextProvider, audioLossRetryDownloadService);

            NavigateBackCommand = ReactiveCommand.CreateFromTask(NavigateBackAsync);
            DownloadProjectCommand = ReactiveCommand.CreateFromTask(DownloadProjectAsync, this.WhenAnyValue(x => x.AllowDownloadProjectCommand));
            CancelOnDownloadingCommand = ReactiveCommand.CreateFromTask(async () => await CancelProjectDownloadAsync(true));
            RetryOnErrorCommand = ReactiveCommand.CreateFromTask(DownloadProjectAsync);
            CancelOnErrorCommand = ReactiveCommand.CreateFromTask(async () => await CancelProjectDownloadAsync(true));
            NavigateOnCompletedCommand = ReactiveCommand.CreateFromTask(NavigateToLoginAsync);
            
            AddProjectState = AddProjectState.None;
            ProjectIdValidationEntry = new ValidationEntryViewModel(
                AppResources.ProjectId,
                viewModelContextProvider,
                false,
                AppResources.Enter32digitId)
            {
                MaxLength = 36,
                OnEnterCommand = DownloadProjectCommand
            };

            Disposables.Add(this.WhenAnyValue(x => x.ProjectIdValidationEntry.Value)
                .Subscribe(value => { AllowDownloadProjectCommand = !string.IsNullOrWhiteSpace(value); }));

            Disposables.Add(this.WhenAnyValue(x => x._addProjectViaIdLocalViewModel.AddProjectState)
                .Subscribe(state => AddProjectState = state));

            Disposables.Add(this.WhenAnyValue(x => x._addProjectViaIdWebViewModel.AddProjectState)
                .Subscribe(state => AddProjectState = state));

            Disposables.Add(this.WhenAnyValue(x => x._addProjectViaIdWebViewModel.AllowProjectOffload)
                .Subscribe(allowProjectOffload => _allowProjectOffload = allowProjectOffload));

            Disposables.Add(this.WhenAnyValue(x => x.AddProjectState)
                .Subscribe(state =>
                {
                    DownloadErrorText = state == AddProjectState.ErrorConnectingToLocalMachine
                        ? AppResources.ErrorConnectLocalMachine
                        : AppResources.ErrorAddingProject;
                }));
        }

        private async Task DownloadProjectAsync()
        {
            //Validate input as guid
            if (ProjectIdValidationEntry.Value.Contains("-"))
            {
                ProjectIdValidationEntry.Value = ProjectIdValidationEntry.Value.Replace("-", "");
            }

            if (!Guid.TryParse(ProjectIdValidationEntry.Value, out var projectId))
            {
                ProjectIdValidationEntry.ValidationMessage = AppResources.ImproperlyFormattedId;
                return;
            }

            AddProjectState = AddProjectState.Loading;
            ProjectId = projectId;
            _allowProjectOffload = true;

            if (await IsProjectAlreadyDownloaded()) return;

            //Check if connected to internet otherwise check if there's connection to other machines on network to sync with.
            if (await ViewModelContextProvider.GetSyncGatewayApiWrapper().IsConnected())
            {
                await _addProjectViaIdWebViewModel.StartProjectDownloadAsync(ProjectId);
            }
            else
            {
                await _addProjectViaIdLocalViewModel.StartProjectDownload(ProjectId);
            }
        }

        private async Task CancelProjectDownloadAsync(bool allowProjectOffload)
        {
            _addProjectViaIdWebViewModel.StopProjectDownload();
            _addProjectViaIdLocalViewModel.StopProjectDownload();

            if (allowProjectOffload && (AddProjectState == AddProjectState.Loading
                                        || AddProjectState == AddProjectState.ErrorAddingProject))
            {
                await _offloadService.OffloadProject(ProjectId);
            }

            AddProjectState = AddProjectState.None;
            ProjectIdValidationEntry.Value = string.Empty;
            ProjectIdValidationEntry.ClearValidation();
            ProjectId = Guid.Empty;
        }

        private async Task<IRoutableViewModel> NavigateToLoginAsync()
        {
            var loginViewModel = await LoginViewModel.CreateAsync(ViewModelContextProvider);
            await loginViewModel.GetAllUsersForLoginAsync();

            return await NavigateToAndReset(loginViewModel);
        }

        private async Task<IRoutableViewModel> NavigateBackAsync()
        {
            await CancelProjectDownloadAsync(_allowProjectOffload);

            return await NavigateBack();
        }

        private async Task<bool> IsProjectAlreadyDownloaded()
        {
            var localProjects = await _localProjectsRepository.GetLocalProjectsForMachine();
            var project = localProjects.GetProject(ProjectId);

            if (project is null) return false;

            switch (project.State)
            {
                case DownloadState.NotStarted:
                case DownloadState.FinishedPartially:
                    return false;

                case DownloadState.Downloading:
                case DownloadState.Offloading:
                case DownloadState.Canceling:
                    await _offloadService.OffloadProject(ProjectId);
                    return false;

                case DownloadState.Finished:
                    AddProjectState = AddProjectState.ProjectAddedSuccessfully;
                    return true;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void Dispose()
        {
            _addProjectViaIdLocalViewModel?.Dispose();
            _addProjectViaIdWebViewModel?.Dispose();

            base.Dispose();
        }
    }

    public enum AddProjectState
    {
        None,
        Loading,
        ErrorAddingProject,
        ErrorConnectingToLocalMachine,
        ProjectAddedSuccessfully
    }
}