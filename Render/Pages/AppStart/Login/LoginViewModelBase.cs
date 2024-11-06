using System.Reactive;
using System.Diagnostics;
using ReactiveUI;
using Render.Components.TitleBar.MenuActions;
using Render.Components.ValidationEntry;
using Render.Kernel;
using Render.Models.LocalOnlyData;
using Render.Models.Users;
using Render.Repositories.LocalDataRepositories;
using Render.Repositories.UserRepositories;
using Render.Resources;
using Render.Services.SyncService;
using Render.Services.UserServices;
using System.Reactive.Linq;
using ReactiveUI.Fody.Helpers;
using Render.Pages.AppStart.Home;
using Render.TempFromVessel.Project;
using Render.Repositories.Kernel;
using Render.WebAuthentication;
using Render.Resources.Localization;
using Render.Pages.AppStart.ProjectSelect;
using Render.Components.Modal;
using Render.Models.Project;
using Version = Render.Kernel.Version;
using Render.Utilities;

namespace Render.Pages.AppStart.Login
{
    public class LoginViewModelBase : PageViewModelBase
    {
        [Reactive] public bool Loading { get; set; }
        [Reactive] public bool ShowAddProjectIdButton { get; set; } = true;
        [Reactive] public bool AdminDownloadFinished { get; set; } = true;
        public ValidationEntryViewModel UsernameViewModel { get; set; }
        public ValidationEntryViewModel PasswordViewModel { get; set; }
        public ReactiveCommand<Unit, IRoutableViewModel> LoginCommand { get; protected set; }

        protected readonly IDataPersistence<Project> ProjectsRepository;
        private readonly ISyncManager _syncManager;
        protected readonly IAuthenticationApiWrapper AuthenticationApiWrapper;
        protected readonly IUserRepository UserRepository;
        protected readonly IMachineLoginStateRepository MachineLoginStateRepository;
        protected readonly ILocalProjectsRepository LocalProjectsRepository;
        protected readonly IUserMachineSettingsRepository UserMachineSettingsRepository;
        private readonly IOffloadService _offloadService;
        private IOneShotReplicator _adminDownloader;
		private readonly IDataPersistence<RenderProject> _renderProjectsRepository;
		private readonly IUserMembershipService _userMembershipService;

		protected LoginViewModelBase(
			string urlPathSegment,
			IViewModelContextProvider viewModelContextProvider,
			string pageName,
			List<IMenuActionViewModel> menuActionViewModels = null)
            : base(urlPathSegment, viewModelContextProvider, pageName, menuActionViewModels)
        {
            AdminDownloadFinished = false;
            UsernameViewModel = new ValidationEntryViewModel(AppResources.Username, viewModelContextProvider, false,
                AppResources.EnterYourUsername);
            PasswordViewModel = new ValidationEntryViewModel(AppResources.Password, viewModelContextProvider, true,
                AppResources.EnterYourPassword);

            _syncManager = viewModelContextProvider.GetSyncManager();
            ProjectsRepository = viewModelContextProvider.GetPersistence<Project>();
			_renderProjectsRepository = viewModelContextProvider.GetPersistence<RenderProject>();
            _offloadService = viewModelContextProvider.GetOffloadService();
            UserRepository = viewModelContextProvider.GetUserRepository();
            AuthenticationApiWrapper = viewModelContextProvider.GetAuthenticationApiWrapper();
            MachineLoginStateRepository = viewModelContextProvider.GetMachineLoginStateRepository();
            LocalProjectsRepository = viewModelContextProvider.GetLocalProjectsRepository();
            UserMachineSettingsRepository = viewModelContextProvider.GetUserMachineSettingsRepository();
			_userMembershipService = viewModelContextProvider.GetUserMembershipService();
		}

		protected async Task<bool> CheckForNewSoftwareVersionAsync(IUser user, Func<Task> onCancelDownloadNewVersion)
		{
#if DEMO
            return false;
#endif

			if (await ViewModelContextProvider.GetSyncGatewayApiWrapper().IsConnected() == false)
			{
				return false;
			}

			var appCenterApi = ViewModelContextProvider.GetAppCenterApiWrapper();
            var availableBetaTesting = await HasProjectForBetaTesting(user);
			var versionString = await appCenterApi.GetLatestVersionAsync(availableBetaTesting);
			var latestVersion = new System.Version(versionString);
			var currentVersion = new System.Version(Version.SoftwareVersionWithFourthNumber);
			var result = latestVersion.CompareTo(currentVersion);

			//If we're running in Rider/VS, the version will be 1.0.0.0 and we don't want the modal to show up then
			if (Version.SoftwareVersionWithFourthNumber != "1.0.0.0" && result > 0)
			{
				await ShowDownloadRenderModal(
                    versionString, appCenterApi.DownloadUrl, onCancelDownloadNewVersion);
                return true;
			}

            return false;
		}

		protected async Task StartSync(IUser user)
		{
#if DEMO
            return;
#endif

			await _syncManager.StartLoginSync(user, ViewModelContextProvider.GetLoggedInUser(), UsernameViewModel.Value, PasswordViewModel.Value);
		}
		
        protected async Task UpdateMachineLogins(Guid userId)
        {
            var machineLoginState = await MachineLoginStateRepository.GetMachineLoginState();
            machineLoginState.AddUserLogIn(userId);
            await MachineLoginStateRepository.SaveMachineLoginState(machineLoginState);
        }

        /// <summary>
        /// Method <c>CleanupProjectsOnMachine</c> removes projects that failed to finish Downloading or Offloading in previous launch from local DB.
        /// </summary>
        protected async Task CleanupProjectsOnMachine() => await _offloadService.OffloadFailedProjects();

        protected async Task SynchronizeAdmin(string username, string password)
        {
            _syncManager.SyncGatewayUser = await AuthenticationApiWrapper.AuthenticateRenderUserForSyncAsync
                (username, password, Guid.Empty);
            
            if (_adminDownloader != null)
            {
                _adminDownloader.DownloadFinished -= FinishLoginWhenDownloadFinished;
                _adminDownloader.Dispose();
            }

            _adminDownloader = _syncManager.GetWebAdminDownloader(
	            _syncManager.SyncGatewayUser.UserId.ToString(),
	            _syncManager.SyncGatewayUser.SyncGatewayPassword,
                ViewModelContextProvider.LocalFolderPath);
            
            _adminDownloader.DownloadFinished += FinishLoginWhenDownloadFinished;
            _adminDownloader.StartDownload();
        }

		private async Task ShowDownloadRenderModal(
            string versionString,
            string downloadRenderUri,
			Func<Task> onCancelDownloadNewVersion)
		{
            if (string.IsNullOrEmpty(downloadRenderUri))
            {
                return;
            }

			await ViewModelContextProvider.GetModalService().ShowInfoModal(
				Icon.DownloadItem,
				string.Format(AppResources.RenderAvailable, versionString),
				string.Format(AppResources.DownloadRenderMessage, versionString),
				okButtonViewModel: new ModalButtonViewModel(AppResources.DownloadRenderButton),
				onClosed: onCancelDownloadNewVersion,
				onOkPressed: async () =>
				{
					await ViewModelContextProvider.GetEssentials().OpenBrowserAsync(downloadRenderUri);
					ViewModelContextProvider.GetCloseApplication()?.Close();
				});
		}

		private async Task<bool> HasProjectForBetaTesting(IUser user)
		{
            if (user is null)
            {
                return false;
            }

			var localProjects = await LocalProjectsRepository.GetLocalProjectsForMachine();
			foreach (var project in localProjects.GetDownloadedProjects())
			{
                var userHasPermissionForProject =
					(user.UserType == UserType.Render
                        && ((RenderUser)user).ProjectId == project.ProjectId)
					|| _userMembershipService.HasExplicitPermissionForProject(user, project.ProjectId);

                if (!userHasPermissionForProject)
                {
                    continue;
                }

				var renderProject = await _renderProjectsRepository
					.QueryOnFieldAsync(nameof(project.ProjectId), project.ProjectId.ToString());
				if (renderProject.IsBetaTester)
				{
					return true;
				}
			}

			return false;
		}

		private void FinishLoginWhenDownloadFinished(bool downloadedSuccessfully)
        {
            if (!downloadedSuccessfully || AdminDownloadFinished)
            {
                return;
            }

            AdminDownloadFinished = true;
            MainThread.BeginInvokeOnMainThread(FinishLoginOnMainThread);
        }

        protected async void FinishLoginOnMainThread()
        {
            await FinishLoginAsync();
            Loading = false;
        }

        protected async Task<IRoutableViewModel> FinishLoginAsync()
        {
            var user = await UserRepository.GetUserAsync(UsernameViewModel.Value);
            if (user == null) return null;

            ViewModelContextProvider.SetLoggedInUser(user);
            await UpdateMachineLogins(user.Id);

            LogInfo("Successful login", new Dictionary<string, string>()
            {
                { "Username", user.Username },
                { "UserType", $"{user.UserType}" }
            });

            await CleanupProjectsOnMachine();

            var projectList = await LocalProjectsRepository.GetLocalProjectsForMachine();
            var projects = projectList.GetProjects();
            var localProjects = projects.Any(x => x.State != DownloadState.NotStarted);

            if (!localProjects)
            {
                return await NavigateToAndReset(
                    await ProjectSelectViewModel.CreateAsync(ViewModelContextProvider));
            }

            await StartSync(user);

            var (hasLastSelectedProject, lastProject) = await GetLastProjectInfo(user);
            
            var lastLocalProject = hasLastSelectedProject
	            ? projects.FirstOrDefault(x => x.ProjectId == lastProject.Id)
	            : null;

            if (lastProject is { IsDeleted: true } && user.UserType == UserType.Render)
            {
                return await ShowProjectDeletedModal();
            }

            if (hasLastSelectedProject is false
                || lastLocalProject == null 
                || lastLocalProject.State == DownloadState.NotStarted
                || lastLocalProject.State == DownloadState.FinishedPartially 
                || (lastProject.IsDeleted && user.UserType == UserType.Vessel))
            {
                return await NavigateToAndReset(
                    await ProjectSelectViewModel.CreateAsync(ViewModelContextProvider));
            }

            //This is a stop gap - if we hit an exception when navigating home, go to the project select page instead.
            try
            {
                var homeViewModel = await HomeViewModel.CreateAsync(lastProject.Id, ViewModelContextProvider);
                return await NavigateToAndReset(homeViewModel);
            }
            catch (Exception e)
            {
                LogError(e);
                return await NavigateToAndReset(
                    await ProjectSelectViewModel.CreateAsync(ViewModelContextProvider));
            }
        }

        protected async Task<(bool hasLastSelectedProject, Project lastProject)> GetLastProjectInfo(IUser user)
        {
	        var userMachineSettings = await UserMachineSettingsRepository.GetUserMachineSettingsForUserAsync(user.Id);
	        
	        var lastProjectId = user.UserType == UserType.Render
		        ? ((RenderUser)user).ProjectId
		        : userMachineSettings.GetLastSelectedProjectId();
	        
	        var lastProject = await ProjectsRepository.GetAsync(lastProjectId);
	        
	        if (lastProject?.IsBetaTester is false)
	        {
		        lastProjectId = Guid.Empty;
		        if (userMachineSettings.GetAndSetLastSelectedProject(lastProjectId))
		        {
			        await UserMachineSettingsRepository.UpdateUserMachineSettingsAsync(userMachineSettings);   
		        }
	        }

	        return (lastProjectId != Guid.Empty, lastProject);
        }

        protected async Task<IRoutableViewModel> ShowProjectDeletedModal()
        {
            await ViewModelContextProvider.GetModalService().ShowInfoModal(
                Icon.TypeWarning,
                AppResources.ProjectDeletedTitle,
                AppResources.NotAbleToLogin);

            return null;
        }

        protected bool IsDemoDatabaseInitialized()
        {
            return DemoHelper.IsDatabaseInitialized;
        }

        [Conditional("DEMO")]
        protected void ShowDemoInitializationError()
        {
            var modalService = ViewModelContextProvider.GetModalService();
            _ = modalService.ShowInfoModal(Icon.InternetError, AppResources.Error, AppResources.ErrorAddingProject);
        }

        public override void Dispose()
        {
            ProjectsRepository?.Dispose();
            UserRepository?.Dispose();
            MachineLoginStateRepository?.Dispose();
            LocalProjectsRepository?.Dispose();
            UserMachineSettingsRepository?.Dispose();
            _syncManager.SyncGatewayUser = null;
            LoginCommand?.Dispose();
            AuthenticationApiWrapper?.Dispose();
            UsernameViewModel?.Dispose();
            PasswordViewModel?.Dispose();

            if (_adminDownloader != null)
            {
                _adminDownloader.DownloadFinished -= FinishLoginWhenDownloadFinished;
                _adminDownloader.Dispose();
            }

            base.Dispose();
        }
    }
}