using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Interfaces;
using Render.Kernel.SyncServices;
using Render.Models.Project;
using Render.Models.Users;
using Render.Repositories.Kernel;
using Render.Repositories.LocalDataRepositories;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Services.SyncService;
using Render.TempFromVessel.Project;
using Render.WebAuthentication;

namespace Render.Kernel;

public class SyncManager : ReactiveObject, ISyncManager
{
    public AuthenticationApiWrapper.SyncGatewayUser SyncGatewayUser { get; set; }

    public bool IsWebSync { get; private set; }

    private Func<Task> PendingFunction { get; set; }

    private readonly List<IDisposable> _disposables = [];

    private readonly IWebSyncService _webWebSyncService;
    private readonly ILocalSyncService _localSyncService;
    private readonly IUsbSyncService _usbSyncService;

    private readonly ILocalProjectsRepository _localProjectsRepository;
    private readonly IDataPersistence<Project> _projectRepository;
    private readonly IDataPersistence<RenderProjectStatistics> _renderProjectStatistics;
    private readonly ISyncGatewayApiWrapper _syncGateWayApiWrapper;
    private readonly IUserMachineSettingsRepository _userMachineSettingsRepository;
    private readonly IAuthenticationApiWrapper _authenticationApiWrapper;
    private readonly IConnectivityService _connectivityService;
    private readonly ISyncStateService _syncStateService;
    private readonly IRenderLogger _renderLogger;

    [Reactive] public CurrentSyncStatus CurrentLocalSyncStatus { get; private set; }
    [Reactive] public CurrentSyncStatus CurrentWebSyncStatus { get; private set; }
    [Reactive] public CurrentSyncStatus CurrentUsbSyncStatus { get; private set; }

    public SyncManager(
        IWebSyncService webWebSyncService,
        ILocalSyncService localSyncService,
        IUsbSyncService usbSyncService,
        ISyncGatewayApiWrapper syncGateWayApiWrapper,
        ILocalProjectsRepository localProjectsRepository,
        IDataPersistence<Project> projectRepository,
        IDataPersistence<RenderProjectStatistics> renderProjectStatistics,
        IUserMachineSettingsRepository userMachineSettingsRepository,
        IAuthenticationApiWrapper authenticationApiWrapper,
        IConnectivityService connectivityService,
        ISyncStateService syncStateService,
        IRenderLogger renderLogger)
    {
        _usbSyncService = usbSyncService;
        _webWebSyncService = webWebSyncService;
        _localSyncService = localSyncService;
        _syncGateWayApiWrapper = syncGateWayApiWrapper;
        _localProjectsRepository = localProjectsRepository;
        _projectRepository = projectRepository;
        _renderProjectStatistics = renderProjectStatistics;
        _userMachineSettingsRepository = userMachineSettingsRepository;
        _authenticationApiWrapper = authenticationApiWrapper;
        _connectivityService = connectivityService;
        _syncStateService = syncStateService;
        _renderLogger = renderLogger;

        this.WhenAnyValue(x => x._webWebSyncService.CurrentSyncStatus)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async webSyncStatus =>
            {
                if (webSyncStatus == CurrentSyncStatus.ActiveReplication)
                {
                    IsWebSync = true;
                }

                if (!IsWebSync) return;
                CurrentWebSyncStatus = webSyncStatus;
                await InvokePendingFunction(webSyncStatus);
            }).ToDisposables(_disposables);

        this.WhenAnyValue(x => x._localSyncService.CurrentSyncStatus)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(localSyncStatus =>
            {
                if (localSyncStatus == CurrentSyncStatus.ActiveReplication)
                {
                    IsWebSync = false;
                }

                if (IsWebSync) return;
                CurrentLocalSyncStatus = localSyncStatus;
            }).ToDisposables(_disposables);

        this.WhenAnyValue(x => x._usbSyncService.CurrentSyncStatus)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async usbSyncStatus =>
            {
                CurrentUsbSyncStatus = usbSyncStatus;
                await InvokePendingFunction(usbSyncStatus);
            }).ToDisposables(_disposables);
    }

    public async Task StartUsbSync(Guid projectId)
    {
        await SetPendingFunction(CurrentWebSyncStatus, async () => await _usbSyncService.StartUsbSync(projectId));
    }

    public async Task StartSync(
        Guid projectId,
        IUser loggedInUser,
        string syncGateWayPassword,
        Action onSyncStarting = null,
        bool needToResetLocalSync = false,
        bool? isInternetAccess = null)
    {
        var lastSyncState = _syncStateService.GetLastSyncState() ?? new LastSynchronizationState();

        var needToStopLocalSync = needToResetLocalSync && lastSyncState.ProjectId != projectId;
        
        lastSyncState.ProjectId = projectId;
        lastSyncState.LoggedInUser = loggedInUser;
        lastSyncState.SyncGatewayPassword = syncGateWayPassword;
        lastSyncState.OnSyncStarting = onSyncStarting;
        
        if (isInternetAccess ?? await _syncGateWayApiWrapper.IsConnected())
        {
            lastSyncState.LastInternetAccess = true;
            await SetPendingFunction(CurrentUsbSyncStatus,
                async () => await StartWebSync(loggedInUser.Id, syncGateWayPassword));
        }
        else
        {
            lastSyncState.LastInternetAccess = false;

            if (needToStopLocalSync)
            {
                _localSyncService.StopLocalSync();
            }

            await _localSyncService.StartLocalSync(loggedInUser.Username, projectId);
            onSyncStarting?.Invoke();
        }

        _syncStateService.SetLastSyncState(lastSyncState);
        await UpdateSyncStatistics();
    }

    public async Task StartLoginSync(IUser user, IUser loggedInUser, string userName, string userPassword)
    {
        if (_connectivityService.Initialized is false)
        {
            _connectivityService.InternetAvailable += OnInternetAccessChanged;
        }

        Guid lastProjectId;
        if (user.UserType == UserType.Vessel)
        {
            var userMachineSettings = await _userMachineSettingsRepository.GetUserMachineSettingsForUserAsync(user.Id);
            lastProjectId = userMachineSettings.GetLastSelectedProjectId();
        }
        else
        {
            lastProjectId = ((RenderUser)user).ProjectId;
        }

        var lastSyncState = new LastSynchronizationState()
        {
            ProjectId = lastProjectId,
            LoggedInUser = loggedInUser,
        };

        var originProject = await _projectRepository.GetAsync(lastProjectId);
        if (!await _syncGateWayApiWrapper.IsConnected())
        {
            if (originProject != null && loggedInUser != null)
            {
                await _localSyncService.StartLocalSync(loggedInUser.Username, originProject.ProjectId);
            }

            lastSyncState.LastInternetAccess = false;
            _syncStateService.SetLastSyncState(lastSyncState);
            return;
        }

        if (string.IsNullOrEmpty(user.SyncGatewayLogin) && SyncGatewayUser == null)
        {
            if (user.UserType == UserType.Vessel)
            {
                SyncGatewayUser = await _authenticationApiWrapper.AuthenticateRenderUserForSyncAsync(userName,
                    userPassword, Guid.Empty);
            }
            else
            {
                var renderUser = (RenderUser)user;

                SyncGatewayUser = await _authenticationApiWrapper.AuthenticateRenderUserForSyncAsync(user.Username, user
                    .HashedPassword, renderUser.ProjectId);

                if ((SyncGatewayUser == null
                     || (string.IsNullOrEmpty(SyncGatewayUser.SyncGatewayPassword) &&
                         SyncGatewayUser.UserId == default))
                    && renderUser.UserSyncCredentials != null)
                {
                    SyncGatewayUser = new AuthenticationApiWrapper.SyncGatewayUser(
                        renderUser.UserSyncCredentials.UserId,
                        renderUser.UserSyncCredentials.UserSyncGatewayLogin);

                    _renderLogger.LogInfo("Render user uses admin credentials for sync");
                }
            }
        }

        var syncGatewayPassword = string.IsNullOrEmpty(user.SyncGatewayLogin)
            ? SyncGatewayUser.SyncGatewayPassword
            : user.SyncGatewayLogin;
        var userId = SyncGatewayUser?.UserId ?? user.Id;

        lastSyncState.LastInternetAccess = true;
        lastSyncState.SyncGatewayPassword = syncGatewayPassword;
        _syncStateService.SetLastSyncState(lastSyncState);

        await StartWebSync(userId, syncGatewayPassword);

        await UpdateSyncStatistics();
    }

    public IOneShotReplicator GetWebAdminDownloader(string syncGatewayUsername, string syncGatewayPassword,
        string databasePath)
    {
        return _webWebSyncService.GetAdminDownloader(syncGatewayUsername, syncGatewayPassword, databasePath);
    }

    public void StopWebSync()
    {
        _webWebSyncService.StopWebSync();
    }

    public void StopLocalSync()
    {
        _localSyncService.StopLocalSync();
    }

    public void StopUsbSync()
    {
        if (CurrentUsbSyncStatus == CurrentSyncStatus.ActiveReplication)
        {
            _usbSyncService.StopUsbSync();
        }
    }

    public void UnsubscribeOnConnectivityChanged()
    {
        if (_connectivityService.Initialized)
        {
            _connectivityService.InternetAvailable -= OnInternetAccessChanged;
        }
    }

    private async Task StartWebSync(Guid userId, string syncGatewayPassword)
    {
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

        //Need to call start all sync here if we didn't do it on login because we were lan connected
        _webWebSyncService.StartWebSync(projectIdList, globalUserIds, userId.ToString(),
            syncGatewayPassword);
    }

    private async Task SetPendingFunction(CurrentSyncStatus currentSyncStatus, Func<Task> syncFunction)
    {
        if (currentSyncStatus is CurrentSyncStatus.ActiveReplication)
        {
            PendingFunction = syncFunction;
            return;
        }

        await syncFunction?.Invoke()!;
    }

    private async Task InvokePendingFunction(CurrentSyncStatus currentSyncStatus)
    {
        if (currentSyncStatus is CurrentSyncStatus.Finished or CurrentSyncStatus.ErrorEncountered
            && PendingFunction != null)
        {
            await PendingFunction.Invoke();
            PendingFunction = null;
        }
    }

    private async Task UpdateSyncStatistics()
    {
        var localProjects = await _localProjectsRepository.GetLocalProjectsForMachine();
        foreach (var project in localProjects.GetDownloadedProjects())
        {
            project.LastSyncDate = DateTime.Now;

            //Update Project Statistics
            var projectStatistics =
                (await _renderProjectStatistics.QueryOnFieldAsync("ProjectId", project.ProjectId.ToString(), 1,
                    false)).FirstOrDefault();

            if (projectStatistics != null &&
                (DateTimeOffset.Now - projectStatistics.RenderProjectLastSyncDate) > TimeSpan.FromMinutes(1))
            {
                projectStatistics.SetRenderProjectLastSyncDate(DateTimeOffset.Now);
                await _renderProjectStatistics.UpsertAsync(projectStatistics.Id, projectStatistics);
            }
        }

        await _localProjectsRepository.SaveLocalProjectsForMachine(localProjects);
    }

    private async void OnInternetAccessChanged(object sender, InternetAvailableEventArgs args)
    {
        var lastSyncState = _syncStateService.GetLastSyncState();

        if (lastSyncState.LastInternetAccess != args.InternetAccess)
        {
            _localSyncService.StopLocalSync();
            _webWebSyncService.StopWebSync();

            _renderLogger.LogInfo("Network access changed", new Dictionary<string, string>()
            {
                { "NetworkAccess", args.InternetAccess.ToString() }
            });

            var syncGatewayPassword = lastSyncState.SyncGatewayPassword ?? lastSyncState.LoggedInUser.SyncGatewayLogin;

            await StartSync(
                lastSyncState.ProjectId,
                lastSyncState.LoggedInUser,
                syncGatewayPassword,
                lastSyncState.OnSyncStarting,
                isInternetAccess: args.InternetAccess);
        }
    }
}