using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Users;
using Render.Pages.AppStart.Home;
using Render.Pages.Settings.ManageUsers;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;
using Render.TempFromVessel.Project;

namespace Render.Pages.Configurator.UserManagement;

public class UserManagementPageViewModel : WorkflowPageBaseViewModel
{
    public DynamicDataWrapper<UserTileViewModel> VesselUserTileViewModels { get; private set; } =
        new DynamicDataWrapper<UserTileViewModel>();

    public DynamicDataWrapper<UserTileViewModel> RenderUserTileViewModels { get; private set; } =
        new DynamicDataWrapper<UserTileViewModel>();

    public ReactiveCommand<Unit, IRoutableViewModel> CreateUserCommand { get; }

    private Project _project;

    public static async Task<UserManagementPageViewModel> CreateAsync(IViewModelContextProvider
            viewModelContextProvider,
        Guid projectId)
    {
        var projectRepository = viewModelContextProvider.GetPersistence<Project>();
        var project = await projectRepository.GetAsync(projectId);
        var userRepository = viewModelContextProvider.GetUserRepository();
        var usersForProject = await userRepository.GetUsersForProjectAsync(project);
        return new UserManagementPageViewModel(viewModelContextProvider, project, usersForProject);
    }

    private UserManagementPageViewModel(IViewModelContextProvider viewModelContextProvider,
        Project project,
        IList<IUser> projectUsers)
        : base("UserManagementPage", viewModelContextProvider, AppResources.ManageUsers, null, null, secondPageName: project.Name)
    {
        var color = (ColorReference)ResourceExtensions.GetResourceValue("SecondaryText");
        if (color != null)
            TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(Icon.User, color.Color, 40)?.Glyph;
        _project = project;
        foreach (var user in projectUsers)
        {
            var vm = new UserTileViewModel(viewModelContextProvider, user, project);
            if (user.UserType == UserType.Vessel)
            {
                VesselUserTileViewModels.Add(vm);
            }
            else if (user.UserType == UserType.Render)
            {
                RenderUserTileViewModels.Add(vm);
            }
        }
        Disposables.Add(VesselUserTileViewModels.Observable
            .MergeMany(item => item.EditUserCommand.IsExecuting)
            .Subscribe(isExecuting => IsLoading = isExecuting));

        Disposables.Add(RenderUserTileViewModels.Observable
            .MergeMany(item => item.EditUserCommand.IsExecuting)
            .Subscribe(isExecuting => IsLoading = isExecuting));

        CreateUserCommand = ReactiveCommand.CreateFromTask(NavigateToCreateUserAsync);
        Disposables.Add(CreateUserCommand.IsExecuting
            .Subscribe(isExecuting => IsLoading = isExecuting));

        ProceedButtonViewModel.SetCommand(NavigateHomeAsync);
        Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
            .Subscribe(isExecuting => IsLoading = isExecuting));
    }

    private async Task<IRoutableViewModel> NavigateToCreateUserAsync()
    {
        var viewModel = await Task.Run(() =>
        {
            var user = new RenderUser("", _project.Id);

            var currentUser = ViewModelContextProvider.GetLoggedInUser();
            user.UserSyncCredentials = new UserSyncCredentials(currentUser.Id, currentUser.SyncGatewayLogin);

            return UserSettingsViewModel.CreateAsync(ViewModelContextProvider, _project.Id, true, user);
        });

        return await NavigateTo(viewModel);
    }

    private async Task<IRoutableViewModel> NavigateHomeAsync()
    {
        var projectId = ViewModelContextProvider.GetGrandCentralStation().CurrentProjectId;
        var homeViewModel = await Task.Run(async () => await HomeViewModel.CreateAsync(projectId, ViewModelContextProvider));
        return await HostScreen.Router.NavigateAndReset.Execute(homeViewModel);
    }

    public void AddUser(IUser user)
    {
        if (user is RenderUser)
        {
            RenderUserTileViewModels.Add(new UserTileViewModel(ViewModelContextProvider, user, _project));
        }
        else
        {
            VesselUserTileViewModels.Add(new UserTileViewModel(ViewModelContextProvider, user, _project));
        }
    }

    public void DeleteUser(IUser user)
    {
        if (user is RenderUser)
        {
            var tile = RenderUserTileViewModels.Items.First(x => x.UserId == user.Id);
            RenderUserTileViewModels.Remove(tile);
        }
        else
        {
            var tile = VesselUserTileViewModels.Items.First(x => x.UserId == user.Id);
            VesselUserTileViewModels.Remove(tile);
        }
    }

    public void UpdateUser(IUser user)
    {
        if (user is RenderUser)
        {
            var tile = RenderUserTileViewModels.Items.First(x => x.UserId == user.Id);
            tile.UpdateUser(user);
        }
        else
        {
            var tile = VesselUserTileViewModels.Items.First(x => x.UserId == user.Id);
            tile.UpdateUser(user);
        }
    }

    public override void Dispose()
    {
        VesselUserTileViewModels?.Dispose();
        VesselUserTileViewModels = null;

        RenderUserTileViewModels?.Dispose();
        RenderUserTileViewModels = null;

        base.Dispose();
    }
}