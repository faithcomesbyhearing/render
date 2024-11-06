using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.Modal;
using Render.Components.Modal.ModalComponents;
using Render.Components.PasswordGrid;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Users;
using Render.Pages.AppStart.Home;
using Render.Pages.AppStart.ProjectSelect;
using Render.Pages.Configurator.UserManagement;
using Render.Repositories.UserRepositories;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;
using Render.TempFromVessel.Project;

namespace Render.Pages.Settings.ManageUsers
{
    public enum PasswordType
    {
        Text,
        Grid
    }

    public class UserSettingsViewModel : WorkflowPageBaseViewModel
    {
        [Reactive] public PasswordGridViewModel PasswordGridViewModel { get; private set; }
        [Reactive] public string Password { get; set; }
        [Reactive] public LocalizationResource SelectedLocalizationResource { get; set; }
        [Reactive] public string UserName { get; set; }
        [Reactive] public LocalizedPasswordTypes SelectedLocalizedPasswordType { get; set; }
        [Reactive] public PasswordType SelectedPasswordType { get; set; }
        [Reactive] public bool HidePassword { get; set; } = true;

        public bool IsRenderUser { get; }
        public bool ShowDeleteButton { get; }
        public string UserTypeString { get; }
        private IUser _user;

        public readonly ReactiveCommand<Unit, Unit> ToggleShowPasswordCommand;
        public readonly ReactiveCommand<Unit, Unit> ResetGridPasswordCommand;
        public readonly ReactiveCommand<Unit, Unit> GeneratePasswordCommand;
        public readonly ReactiveCommand<Unit, IRoutableViewModel> SaveUserCommand;
        public readonly ReactiveCommand<Unit, IRoutableViewModel> DeleteUserCommand;
        private readonly IUserRepository _userRepository;
        public List<LocalizationResource> LocalizationResources { get; } = new List<LocalizationResource>();
        private bool isCreate;
        private Project _project;

        public static async Task<UserSettingsViewModel> CreateAsync(
            IViewModelContextProvider viewModelContextProvider,
            Guid projectId,
            bool createMode,
            IUser user = null)
        {
            Project project = default;
            if (projectId != Guid.Empty)
            {
                var projectRepository = viewModelContextProvider.GetPersistence<Project>();
                project = await projectRepository.GetAsync(projectId);
            }

            return new UserSettingsViewModel(viewModelContextProvider, project, createMode, user);
        }

        public List<LocalizedPasswordTypes> PasswordTypes { get; } = new List<LocalizedPasswordTypes>
        {
            new LocalizedPasswordTypes(PasswordType.Grid, AppResources.GridPassword),
            new LocalizedPasswordTypes(PasswordType.Text, AppResources.TextPassword)
        };

        private UserSettingsViewModel(IViewModelContextProvider viewModelContextProvider, Project project, bool createMode, IUser user = null) :
            base("UserSettings", viewModelContextProvider, AppResources.ManageUsers, null, null, secondPageName: project == null ? "" : project.Name)
        {
            isCreate = createMode;
            _project = project;
            _user = user ?? viewModelContextProvider.GetLoggedInUser();
            UserTypeString = _user.UserType == UserType.Render ? AppResources.Render : AppResources.Global;
            UserTypeString = UserTypeString.ToUpper();
            UserName = _user.FullName;
            SelectedLocalizedPasswordType = _user.IsGridPassword
                ? PasswordTypes.First(x => x.PasswordType == PasswordType.Grid)
                : PasswordTypes.First(x => x.PasswordType == PasswordType.Text);
            SelectedPasswordType = SelectedLocalizedPasswordType.PasswordType;
            var color = (ColorReference)ResourceExtensions.GetResourceValue("SecondaryText");
            if (color != null)
                TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(Icon.Project, color.Color, 35)?.Glyph;
            IsRenderUser = _user.UserType == UserType.Render;
            ShowDeleteButton = viewModelContextProvider.GetLoggedInUser()?.UserType == UserType.Vessel && IsRenderUser;
            ResetGridPasswordCommand = ReactiveCommand.Create(ResetGridPassword);
            GeneratePasswordCommand = ReactiveCommand.Create(GeneratePassword);
            SaveUserCommand = ReactiveCommand.CreateFromTask(SaveUserAsync);
            DeleteUserCommand = ReactiveCommand.CreateFromTask(DeleteUserAsync);
            ToggleShowPasswordCommand = ReactiveCommand.Create(ToggleShowPassword);
            var gridPassword = _user.IsGridPassword ? _user.HashedPassword ?? "" : "";
            PasswordGridViewModel = new PasswordGridViewModel(viewModelContextProvider, gridPassword);
            ProceedButtonViewModel.SetCommand(SaveUserAsync);
            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));
            _userRepository = viewModelContextProvider.GetUserRepository();
            Disposables.Add(this.WhenAnyValue(x => x.PasswordGridViewModel.Password)
                .Subscribe(gridPW => { Password = gridPW; }));
            var availableCultures = Utilities.Utilities.GetAvailableCultures().OrderBy(x => x.DisplayName);
            Disposables.Add(this.WhenAnyValue(x => x.SelectedLocalizedPasswordType)
                .Skip(1)
                .Subscribe(t =>
                {
                    SelectedPasswordType = t.PasswordType;
                    if (t.PasswordType == PasswordType.Grid)
                    {
                        PasswordGridViewModel.ResetPassword();
                    }

                    Password = "";
                }));
            Disposables.Add(this.WhenAnyValue(x => x.Password, x => x.UserName)
                .Subscribe(DetermineProceedButtonActive));

            if (!_user.IsGridPassword)
            {
                Password = _user.HashedPassword;
            }

            foreach (var culture in availableCultures)
            {
                var resource = new LocalizationResource(culture.Name, culture.NativeName);
                LocalizationResources.Add(resource);
                if (_user.LocaleId == culture.Name)
                {
                    SelectedLocalizationResource = resource;
                }
            }
        }

        private void ResetGridPassword()
        {
            PasswordGridViewModel?.ResetPassword();
        }

        private void GeneratePassword()
        {
            var passwordService = ViewModelContextProvider.GetPasswordService();
            Password = passwordService.GeneratePassword();
        }

        private void DetermineProceedButtonActive((string password, string name) value)
        {
            //validate that the username is not empty and that a text password is not empty
            //or that a grid password has more than one grid spot chosen
            ProceedButtonViewModel.ProceedActive = !string.IsNullOrEmpty(value.name) &&
                                                   (SelectedPasswordType == PasswordType.Text &&
                                                    !string.IsNullOrEmpty(Password)
                                                    || Password?.Length > 2);
        }

        private async Task<IRoutableViewModel> SaveUserAsync()
        {
            try
            {
                var changesMade = isCreate;
                if (SelectedLocalizationResource != null && SelectedLocalizationResource.Resource != _user.LocaleId)
                {
                    changesMade = true;
                    _user.LocaleId = SelectedLocalizationResource.Resource;
                    // change app localization only when the edited user is loggedin user
                    if (_user.Id == GetLoggedInUserId())
                    {
                        var localizationService = ViewModelContextProvider.GetLocalizationService();
                        localizationService.SetLocalization(_user.LocaleId);
                    }
                }

                if (_user.UserType == UserType.Render && _user.FullName != UserName)
                {
                    changesMade = true;
                    _user.Username = UserName;
                    _user.FullName = UserName;
                }

                if (_user.UserType == UserType.Render && Password != "" && _user.HashedPassword != Password)
                {
                    changesMade = true;
                    _user.HashedPassword = Password;
                    _user.IsGridPassword = SelectedPasswordType == PasswordType.Grid;
                    if (IsRenderUser && ((RenderUser)_user).UserSyncCredentials == null)
                    {
                        await Task.Run(() =>
                        {
                            var userSyncCredentials =
                                new UserSyncCredentials(_user.Id, _user.SyncGatewayLogin);
                            ((RenderUser)_user).UserSyncCredentials = userSyncCredentials;
                            return Task.CompletedTask;
                        });
                    }
                }

                if (!changesMade) return await NavigateBack();

                await _userRepository.SaveUserAsync(_user);
#if !DEMO
                await ViewModelContextProvider.GetSyncManager().StartSync(GetProjectId(),
                    _user,
                    _user.SyncGatewayLogin);
#endif
                var userManagementViewModel =
                    HostScreen.Router.NavigationStack.FirstOrDefault(x => x is UserManagementPageViewModel);
                if (userManagementViewModel != null)
                {
                    return await NavigateToUserSettings(userManagementViewModel);
                }

                if (HostScreen.Router.NavigationStack.IsPreviousScreenHome())
                {
                    return await FinishCurrentStackAndNavigateHome();
                }

                //The only other way to get to this page is via the Project Select screen
                var projectSelectViewModel = await ProjectSelectViewModel.CreateAsync(ViewModelContextProvider);
                return await NavigateToAndReset(projectSelectViewModel);
            }
            catch (Exception e)
            {
                LogError(e, new Dictionary<string, string>()
                {
                    { "Failure", "Save user failed" }
                });
                throw;
            }
        }

        private async Task<IRoutableViewModel> DeleteUserAsync()
        {
            if (_user.UserType == UserType.Render)
            {
                if (isCreate)
                {
                    return await NavigateBackCommand.Execute();
                }

                var workflowRepository = ViewModelContextProvider.GetWorkflowRepository();
                var workflow = await workflowRepository.GetWorkflowForProjectIdAsync(_project.Id);
                var teams = workflow.GetTeams();
                var translatorTeam = teams.FirstOrDefault(x => x.TranslatorId == _user.Id);
                var userTeam = translatorTeam is null ? 
                               teams.FirstOrDefault(team => team.WorkflowAssignments.Select(x => x.UserId).Any(x => x == _user.Id)) :
                               translatorTeam;

                if (userTeam != null)
                {
                    var sectionRepository = ViewModelContextProvider.GetSectionRepository();
                    var modalService = ViewModelContextProvider.GetModalService();
                    var allSections = await sectionRepository.GetSectionsForProjectAsync(_project.Id);
                    var enableSectionAssignment = teams.All(team => team.TranslatorId != Guid.Empty && allSections.Count != 0);

                    var deleteUserComponent = new DeleteUserComponentViewModel(
                        ViewModelContextProvider,
                        userTeam.Id,
                        _user.FullName,
                        enableSectionAssignment);

                    Disposables.Add(deleteUserComponent.ViewSectionAssignmentsCommand.IsExecuting
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(isExecuting =>
                        {
                            IsLoading = isExecuting;
                            if (isExecuting)
                            {
                                modalService.Close(DialogResult.Cancel);
                            }
                        }));

                    var confirmationResult = await modalService.ConfirmationModal(
                        Icon.DeleteWarning,
                        AppResources.DeleteUserTitle,
                        deleteUserComponent,
                        new ModalButtonViewModel(AppResources.Cancel),
                        new ModalButtonViewModel(AppResources.OK));

                    if (confirmationResult != DialogResult.Ok)
                    {
                        return null;
                    }

                    var assignments = userTeam.GetWorkflowAssignmentsForUser(_user.Id);
                    if (assignments is not null)
                    {
                        foreach (var assignment in assignments)
                        {
                            userTeam.RemoveAssignment(assignment.StageId, assignment.Role);
                        }
                    }

                    if (userTeam.TranslatorId == _user.Id)
                    {
                        userTeam.RemoveTranslator();
                    }

                    await workflowRepository.SaveWorkflowAsync(workflow);
                }
            }

            await _userRepository.DeleteUserAsync(_user);
            var userManagementViewModel =
                HostScreen.Router.NavigationStack.FirstOrDefault(x => x is UserManagementPageViewModel);
            return await NavigateToUserSettings(userManagementViewModel, true);
        }

        private async Task<IRoutableViewModel> NavigateToUserSettings(IRoutableViewModel userManagementPageViewModel, bool isDelete = false)
        {
            if (isCreate)
            {
                ((UserManagementPageViewModel)userManagementPageViewModel)?.AddUser(_user);
            }
            else if (isDelete)
            {
                ((UserManagementPageViewModel)userManagementPageViewModel)?.DeleteUser(_user);
            }
            else
            {
                ((UserManagementPageViewModel)userManagementPageViewModel)?.UpdateUser(_user);
            }

            return await NavigateBack();
        }

        private void ToggleShowPassword()
        {
            HidePassword = !HidePassword;
        }
    }

    public class LocalizationResource
    {
        public string Resource { get; }
        public string ResourceDisplayName { get; }

        public LocalizationResource(string resource, string resourceDisplayName)
        {
            Resource = resource;
            ResourceDisplayName = resourceDisplayName;
        }
    }

    public class LocalizedPasswordTypes
    {
        public PasswordType PasswordType { get; private set; }
        public string LocalizedPasswordType { get; private set; }

        public LocalizedPasswordTypes(PasswordType passwordType, string localizedPasswordType)
        {
            PasswordType = passwordType;
            LocalizedPasswordType = localizedPasswordType;
        }
    }
}