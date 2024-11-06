using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Microsoft.AspNetCore.Identity;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.PasswordGrid;
using Render.Components.ProfileAvatar;
using Render.Kernel;
using Render.Models.LocalOnlyData;
using Render.Models.Users;
using Render.Pages.AppStart.Home;
using Render.Pages.AppStart.ProjectSelect;
using Render.Resources.Localization;
using Render.Services.PasswordServices;
using Render.Services.UserServices;
using Render.WebAuthentication;

namespace Render.Pages.AppStart.Login
{
    public class LoginViewModel : LoginViewModelBase
    {
        [Reactive] public bool ShowIconLogin { get; private set; }

        [Reactive] public bool ShowIconPassword { get; private set; }

        [Reactive] public bool ShowGrid { get; private set; }

        [Reactive] public PasswordGridViewModel PasswordGridViewModel { get; set; }

        [Reactive] public UserLoginIconViewModel UserLoginIconViewModel { get; private set; }

        [Reactive] public ReactiveCommand<Unit, Unit> BackButtonCommand { get; set; }

        [Reactive] public ReactiveCommand<Unit, IRoutableViewModel> AddNewUserCommand { get; set; }

        [Reactive] public ReactiveCommand<Unit, IRoutableViewModel> TryLoginCommand { get; set; }

        [Reactive] public ReactiveCommand<Unit, Unit> ViewAllUsersCommand { get; set; }

        [Reactive] public bool ShowBackButton { get; set; }
        [Reactive] public bool ShowAllUsers { get; set; }


        //List of 8
        private SourceCache<UserLoginIconViewModel, Guid> UserViewModelSourceList { get; } = new(x => x.User.Id);

        private readonly ReadOnlyObservableCollection<UserLoginIconViewModel> _userLoginViewModels;
        public ReadOnlyObservableCollection<UserLoginIconViewModel> UserLoginViewModels => _userLoginViewModels;

        //List of last 4 users logged in
        private SourceCache<UserLoginIconViewModel, Guid> TopUserViewModelSourceList { get; } = new(x => x.User.Id);

        private readonly ReadOnlyObservableCollection<UserLoginIconViewModel> _topUserLoginViewModels;
        public ReadOnlyObservableCollection<UserLoginIconViewModel> TopUserLoginViewModels => _topUserLoginViewModels;

        private readonly IUserMembershipService _userMembershipService;

        private bool NoDataOnMachine { get; set; }

        public static async Task<LoginViewModel> CreateAsync(IViewModelContextProvider contextProvider)
        {
            var loginViewModel = new LoginViewModel(contextProvider);
            await loginViewModel.GetUsersForLoginAsync();
            return loginViewModel;
        }

        /// <summary>
        /// This constructor is private. Always use the static CreateAsync() method to get this viewmodel, so that the
        /// async initialization of the viewmodel can occur before you navigate.
        /// </summary>
        private LoginViewModel(IViewModelContextProvider viewModelContextProvider)
            : base("Login", viewModelContextProvider, "Login")
        {
            _userMembershipService = viewModelContextProvider.GetUserMembershipService();

            PasswordGridViewModel = new PasswordGridViewModel(ViewModelContextProvider, "");

            var changeList = UserViewModelSourceList
                .Connect()
                .Publish();
            Disposables.Add(new List<IDisposable>
            {
                changeList
                    .Sort(SortExpressionComparer<UserLoginIconViewModel>
                        .Ascending(x => x.User.FullName))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Bind(out _userLoginViewModels)
                    .Subscribe(),
                changeList
                    .WhenPropertyChanged(s => s.Selected, false)
                    .Select(c => c.Sender)
                    .Subscribe(SetSelectedUser),
                changeList.Connect()
            });

            var topChangeList = TopUserViewModelSourceList.Connect().Publish();
            Disposables.Add(new List<IDisposable>
            {
                topChangeList.ObserveOn(RxApp.MainThreadScheduler)
                    .Bind(out _topUserLoginViewModels)
                    .Subscribe(),
                topChangeList.WhenPropertyChanged(s => s.Selected, false)
                    .Select(c => c.Sender)
                    .Subscribe(SetSelectedUser),
                topChangeList.Connect()
            });

            Disposables.Add(this.WhenAnyValue(x => x.UserLoginIconViewModel)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(vm =>
                {
                    if (vm == null)
                    {
                        ShowIconLogin = true;
                        ShowIconPassword = false;
                        PasswordViewModel.PutEntryInFocus(false);
                    }
                    else
                    {
                        ShowIconLogin = false;
                        ShowIconPassword = true;
                        PasswordViewModel.PutEntryInFocus(true);
                    }
                }));
            Disposables.Add(this.WhenAnyValue(x => x.ShowIconPassword)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => ShowBackButton = ShowAllUsers || x));
            Disposables.Add(this.WhenAnyValue(x => x.UsernameViewModel.Value)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(s =>
            {
                if (!UsernameViewModel.CheckValidation() && !string.IsNullOrEmpty(s))
                {
                    UsernameViewModel.ClearValidation();
                }
            }));

            PasswordViewModel.OnEnterCommand = ReactiveCommand.CreateFromTask(async () => { await TryLoginAsync(); });
            TryLoginCommand = ReactiveCommand.CreateFromTask(TryLoginAsync);
            BackButtonCommand = ReactiveCommand.CreateFromTask(ClearSelectedUserOrViewAllUsersAsync);
            AddNewUserCommand = ReactiveCommand.CreateFromTask(AddNewUserAsync);
            ViewAllUsersCommand = ReactiveCommand.CreateFromTask(ViewAllUsers);
        }

        private void SetSelectedUser(UserLoginIconViewModel vm)
        {
            if (vm.Selected && UserLoginIconViewModel == null)
            {
                ShowAllUsers = false;
                UsernameViewModel.Value = vm.User.Username;
                UserLoginIconViewModel = vm;
                ShowGrid = vm.User.IsGridPassword;
                PasswordGridViewModel.ResetPassword();
                PasswordGridViewModel.ResetValidation();
            }
            else if (UserLoginIconViewModel == vm)
            {
                UsernameViewModel.Value = "";
                UserLoginIconViewModel = null;
                ShowGrid = false;
            }
        }

        private async Task GetUsersForLoginAsync()
        {
            var machineState = await MachineLoginStateRepository.GetMachineLoginState();
            var users = await UserRepository.GetAllUsersAsync();
            
            foreach (var user in users)
            {
                if (user is not RenderUser renderUser) continue;
                
                var renderProject = await ProjectsRepository.GetAsync(renderUser.ProjectId);
                    
                if (renderProject.IsBetaTester) continue;
                
                machineState.RemoveUser(renderUser.Id);
                await MachineLoginStateRepository.SaveMachineLoginState(machineState);
            }

            var top4Ids = machineState.GetTopFourUserIds().ToList();
            
            foreach (var userId in top4Ids)
            {
                var user = users.FirstOrDefault(x => x.Id == userId);
                if (user == null)
                {
                    machineState.RemoveUser(userId);
                }
                else
                {
                    TopUserViewModelSourceList.AddOrUpdate(new UserLoginIconViewModel(user, ViewModelContextProvider));
                }
            }

            var lastLoggedInUser = TopUserViewModelSourceList.Items.LastOrDefault()?.User;
            if (lastLoggedInUser != null)
            {
                var localizationService = ViewModelContextProvider.GetLocalizationService();
                localizationService.SetLocalization(lastLoggedInUser.LocaleId);
            }

            var projectId = machineState.LastProjectId;
            if (projectId == Guid.Empty) return;
            var project = await ProjectsRepository.GetAsync(projectId);

            var projectUsers = users.Where(x =>
                    (_userMembershipService.HasExplicitPermissionForProject(x, project.Id)
                     || x is RenderUser u && u.ProjectId == project.Id)
                    && !top4Ids.Contains(x.Id))
                .OrderBy(x => x.Username).Take(8).ToList();

            foreach (var user in projectUsers)
            {
                if (project.IsBetaTester is false) continue;

                var userViewModel = new UserLoginIconViewModel(user, ViewModelContextProvider);
                UserViewModelSourceList.AddOrUpdate(userViewModel);
            }

            var keysToRemove = UserViewModelSourceList.Items
                .Where(x => !projectUsers.Contains(x.User))
                .Select(x => x.User.Id)
                .ToList();
            UserViewModelSourceList.RemoveKeys(keysToRemove);
        }

        private async Task ResetUsersToTop8Async()
        {
            ShowAllUsers = false;
            ShowBackButton = false;
            await GetUsersForLoginAsync();
        }

        public async Task<int> GetAllUsersForLoginAsync()
        {
            ShowAllUsers = true;
            ShowBackButton = true;
            var users = await UserRepository.GetAllUsersAsync();
            var localProjects = await LocalProjectsRepository.GetLocalProjectsForMachine();

            foreach (var user in users)
            {
                if (user is RenderUser renderUser)
                {
                    if (localProjects.CheckForDownloadedProject(renderUser.ProjectId))
                    {
                        var project = await ProjectsRepository.GetAsync(renderUser.ProjectId);
                        if (project.IsBetaTester is false)
                        {
                            continue;
                        }

                        var userViewModel = new UserLoginIconViewModel(user, ViewModelContextProvider);
                        UserViewModelSourceList.AddOrUpdate(userViewModel);
                    }
                }
                else if (user is User)
                {
                    foreach (var projectId in localProjects.GetProjectIds())
                    {
                        if (_userMembershipService.HasExplicitPermissionForProject(user, projectId))
                        {
                            var userViewModel = new UserLoginIconViewModel(user, ViewModelContextProvider);
                            UserViewModelSourceList.AddOrUpdate(userViewModel);
                            break;
                        }
                    }
                }
            }

            return UserViewModelSourceList.Count;
        }

        public async Task<IRoutableViewModel> AddNewUserAsync()
        {
            var vm = await Task.Run(() => Task.FromResult(new AddVesselUserLoginViewModel(ViewModelContextProvider)));
            return await NavigateTo(vm);
        }

        private async Task ViewAllUsers()
        {
            await GoBackAsync();
            await GetAllUsersForLoginAsync();
        }

        /// <summary>
        /// In order to test the navigation of this task, return an IRoutableViewModel to test against. Since the end
        /// result of the task is a navigation, returning this doesn't hurt anything. TestBase has a way to make sure you're
        /// routing to the correct viewmodel.
        /// </summary>
        public async Task<IRoutableViewModel> TryLoginAsync()
        {
#if DEMO
            if (IsDemoDatabaseInitialized() is false)
            {
                ShowDemoInitializationError();
                return null;
            }
#endif
            try
            {
                PasswordViewModel.ClearValidation();
                if (!ShowGrid && string.IsNullOrEmpty(PasswordViewModel.Value) ||
                    ShowGrid && string.IsNullOrEmpty(PasswordGridViewModel.Password))
                {
                    PasswordViewModel.SetValidation(AppResources.EmptyPassword);
                }
                else
                {
                    Loading = true;
                    IsLoading = true;

                    var user = await UserRepository.GetUserAsync(UserLoginIconViewModel.User.Id);
                    if (user != null)
                    {
                        var validatorFactory = new PasswordValidatorFactory();
                        var validator = validatorFactory.GetValidator(user);
                        var result = validator.ValidatePassword(user,
                            ShowGrid ? PasswordGridViewModel.Password : PasswordViewModel.Value);

                        if (result == PasswordVerificationResult.Success ||
                            result == PasswordVerificationResult.SuccessRehashNeeded)
                        {
                            PasswordViewModel.ClearValidation();
                            if (await CheckForNewSoftwareVersionAsync(
                                    user,
                                    () => AfterPasswordVerificationSuccess(user)))
                            {
                                IsLoading = false;
                                return null;
                            }

                            return await AfterPasswordVerificationSuccess(user);
                        }

                        if (result == PasswordVerificationResult.Failed)
                        {
                            var authResult = await TryLoginThroughAuthenticationService(user);
                            if (authResult.SignInResult)
                            {
                                if (await CheckForNewSoftwareVersionAsync(user, null))
                                {
                                    IsLoading = false;
                                }

                                return null;
                            }
                        }

                        if (user.IsGridPassword)
                        {
                            PasswordGridViewModel.SetValidation(AppResources.IncorrectPattern);
                        }
                        else
                        {
                            PasswordViewModel.SetValidation(AppResources.IncorrectPassword);
                        }

                        LogInfo("Login failed", new Dictionary<string, string>
                        {
                            { "User Name", user.Username }
                        });

                        Loading = false;
                        IsLoading = false;

                        return null;
                    }

                    if (!PasswordViewModel.CheckValidation())
                    {
                        PasswordViewModel.ClearValidation();
                    }
                }
            }
            catch (Exception e)
            {
                IsLoading = false;
                var properties = new Dictionary<string, string>()
                {
                    { "Page", "Login" },
                    { "Username", UsernameViewModel.Value },
                    { "Device Has No Data", NoDataOnMachine.ToString() }
                };
                LogError(e);
                throw;
            }

            return new LoginViewModel(ViewModelContextProvider);
        }

        private async Task<IRoutableViewModel> AfterPasswordVerificationSuccess(IUser user)
        {
            IsLoading = true;
            var vesselUser = user.UserType == UserType.Vessel
                             && await ViewModelContextProvider.GetSyncGatewayApiWrapper().IsConnected();
            if (!vesselUser)
            {
                return await RenderUserLogin(user);
            }

            var authResult = await TryLoginThroughAuthenticationService(user);
            if (authResult.OfflineError)
            {
                return await RenderUserLogin(user);
            }

            return null;
        }

        private async Task<IRoutableViewModel> RenderUserLogin(IUser user)
        {
            IsLoading = true;

            return await FinishLoginAsync(user);
        }

        private async Task<AuthenticationApiWrapper.AuthenticationResult> TryLoginThroughAuthenticationService(IUser user)
        {
            var result =
                await AuthenticationApiWrapper.AuthenticateUserAsync(UsernameViewModel.Value, PasswordViewModel.Value);

            if (result.SignInResult)
            {
                IsLoading = true;
                if (user.UserType == UserType.Vessel && await ViewModelContextProvider.GetSyncGatewayApiWrapper().IsConnected())
                {
                    await SynchronizeAdmin(UsernameViewModel.Value, PasswordViewModel.Value);
                }
            }

            return result;
        }

        private async Task<IRoutableViewModel> FinishLoginAsync(IUser user)
        {
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
                var vm = await ProjectSelectViewModel.CreateAsync(ViewModelContextProvider);

                Loading = false;

                return await NavigateToAndReset(vm);
            }

            await StartSync(user);

            var (hasLastSelectedProject, lastProject) = await GetLastProjectInfo(user);

            var lastLocalProject = hasLastSelectedProject
                ? projects.FirstOrDefault(x => x.ProjectId == lastProject.Id)
                : null;

            if (lastProject is { IsDeleted: true } && user.UserType == UserType.Render)
            {
                Loading = false;
                return await ShowProjectDeletedModal();
            }

            if (hasLastSelectedProject is false
                || lastLocalProject == null
                || lastLocalProject.State == DownloadState.NotStarted
                || (lastProject.IsDeleted && user.UserType == UserType.Vessel))
            {
                var vm = await ProjectSelectViewModel.CreateAsync(ViewModelContextProvider);

                Loading = false;

                return await NavigateToAndReset(vm);
            }

            //This is a stop gap - if we hit an exception when navigating home, go to the project select page instead.
            try
            {
                var homeViewModel = await HomeViewModel.CreateAsync(lastProject.Id, ViewModelContextProvider);

                Loading = false;

                return await NavigateToAndReset(homeViewModel);
            }
            catch (Exception e)
            {
                LogError(e);

                LogInfo("Failed Home navigation", new Dictionary<string, string>
                {
                    { "Username", user.Username },
                    { "UserType", $"{user.UserType}" }
                });

                var vm = await ProjectSelectViewModel.CreateAsync(ViewModelContextProvider);

                Loading = false;

                return await NavigateTo(vm);
            }
        }

        private async Task ClearSelectedUserOrViewAllUsersAsync()
        {
            PasswordViewModel.Value = string.Empty;
            PasswordViewModel.IsPassword = true;
            PasswordViewModel.ClearValidation();

            if (UserLoginIconViewModel != null)
            {
                UserLoginIconViewModel.Selected = false;
                UserViewModelSourceList.RemoveKeys(TopUserViewModelSourceList.Keys);
            }
            else
            {
                await ResetUsersToTop8Async();
            }
        }

        public async Task GoBackAsync()
        {
            if (ShowIconPassword)
            {
                await ClearSelectedUserOrViewAllUsersAsync();
            }
        }

        public override void Dispose()
        {
            foreach (var viewModel in UserLoginViewModels)
            {
                viewModel.Dispose();
            }

            BackButtonCommand?.Dispose();
            UserViewModelSourceList?.Dispose();
            TopUserViewModelSourceList?.Dispose();
            UserLoginIconViewModel?.Dispose();
            PasswordGridViewModel?.Dispose();

            base.Dispose();
        }
    }
}