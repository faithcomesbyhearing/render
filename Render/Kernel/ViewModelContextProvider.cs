using Render.WebAuthentication;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.Maui;
using Render.Components.AudioRecorder;
using Render.Components.BarPlayer;
using Render.Components.DivisionPlayer;
using Render.Components.MiniWaveformPlayer;
using Render.Components.DraftSelection;
using Render.Components.TitleBar;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Audio;
using Render.Models.LocalOnlyData;
using Render.Repositories.Kernel;
using Render.Repositories.LocalDataRepositories;
using Render.Repositories.SectionRepository;
using Render.Services;
using Render.Services.SyncService;
using Render.Services.UserServices;
using Render.TempFromVessel.AdministrativeGroups;
using Render.TempFromVessel.Kernel;
using Render.TempFromVessel.Project;
using Render.Models.Sections;
using Render.Models.Sections.CommunityCheck;
using Render.Models.Snapshot;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Repositories.Audio;
using Render.Repositories.SnapshotRepository;
using Render.Repositories.UserRepositories;
using Render.Repositories.WorkflowRepositories;
using Render.Services.AudioServices;
using Render.Services.FileNaming;
using Render.Services.SessionStateServices;
using Render.Interfaces.AudioServices;
using Render.Interfaces.EssentialsWrappers;
using Render.Pages.AppStart.SplashScreen;
using Render.Services.PasswordServices;
using Splat;
using Render.Interfaces;
using Render.Utilities;
using Microsoft.AppCenter;
using Render.Services.WaveformService;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer;
using Render.Interfaces.WrappersAndExtensions;
using Render.Kernel.SyncServices;
using Render.Models.Project;
using Render.Services.AudioPlugins.AudioPlayer;
using Render.Services.EntityChangeListenerServices;
using Render.Services.GrandCentralStation;
using Render.Services.InterpretationService;
using Render.Services.SectionMovementService;
using Render.Services.SyncService.DbFolder;
using Render.Services.SnapshotService;
using Render.Services.StageService;
using Render.Services.WorkflowService;

namespace Render.Kernel
{
    public partial class ViewModelContextProvider : IViewModelContextProvider
    {
        private ISyncManager SyncManager { get; set; }
        private IWebSyncService WebSyncService { get; }
        private ILocalSyncService LocalSyncService { get; }
        private ISyncGatewayApiWrapper SyncGatewayApiWrapper { get; }
        private IConnectivityService ConnectivityService { get; }
        private IGrandCentralStation GrandCentralStation { get; }
        private IWorkflowService WorkflowService { get; }
        private IStageService StageService { get; }
        private ISectionMovementService SectionMovementService { get; }
        private IUser LoggedInUser { get; set; }
        private IEssentialsWrapper EssentialsWrapper { get; }
        private IDateTimeWrapper DateTimeWrapper { get; }
        private ISessionStateService SessionStateService { get; }
        private IDatabaseWrapper RenderDatabase { get; }
        private IDatabaseWrapper RenderAudioDatabase { get; }
        private IDatabaseWrapper LocalDatabase { get; }
        private ISequencerFactory SequencerFactory { get; }

        public IEntityChangeListenerService UserChangeListenerService { get; set; }

        private string _localFolderPath;

        public string LocalFolderPath
        {
            get
            {
                if (_localFolderPath is null)
                {
                    _localFolderPath = Locator.Current.GetService<IAppDirectory>().AppData;
                }

                return _localFolderPath;
            }
        }

        public Page CreateLaunchPage(RoutingState routingState)
        {
            var splashViewModel = new SplashScreenViewModel(this);
            routingState.Navigate.Execute(splashViewModel).Subscribe();

            return new RoutedViewHost();
        }

        private IDatabaseWrapper GetDatabase<T>(IBucketMapper bucketMapper)
        {
            var databaseName = bucketMapper.GetBucketName<T>();
            if (databaseName == Buckets.render.ToString())
            {
                return RenderDatabase;
            }

            if (databaseName == Buckets.renderaudio.ToString())
            {
                return RenderAudioDatabase;
            }

            if (databaseName == Buckets.localonlydata.ToString())
            {
                return LocalDatabase;
            }

            return null;
        }

        public async Task CompactDatabasesAsync()
        {
            await Task.Run(() =>
            {
                RenderDatabase?.CompactDatabase();
                RenderAudioDatabase?.CompactDatabase();
                LocalDatabase?.CompactDatabase();
            });
        }

        public ISyncGatewayApiWrapper GetSyncGatewayApiWrapper()
        {
            return SyncGatewayApiWrapper;
        }

        public IGrandCentralStation GetGrandCentralStation()
        {
            return GrandCentralStation;
        }

        public IUser GetLoggedInUser()
        {
            return LoggedInUser;
        }

        public IEssentialsWrapper GetEssentials()
        {
            return EssentialsWrapper;
        }

        public IUserMachineSettingsRepository GetUserMachineSettingsRepository()
        {
            return new UserMachineSettingsRepository(GetPersistence<UserMachineSettings>());
        }

        public ILocalProjectsRepository GetLocalProjectsRepository()
        {
            return new LocalProjectsRepository(GetPersistence<LocalProjects>());
        }

        public ISectionRepository GetSectionRepository()
        {
            return new SectionRepository(
                GetLogger(typeof(SectionRepository)),
                GetPersistence<Section>(),
                GetSectionReferenceAudioRepository(),
                GetDraftRepository(),
                GetAudioRepository(),
                GetRetellBackTranslationRepository(),
                GetSegmentBackTranslationRepository(),
                GetNotableAudioRepository(),
                GetResponseRepository(),
                GetCommunityRetellAudioRepository(),
                GetPersistence<Reference>(),
                GetCommunityTestRepository());
        }

        public IAudioRepository<SectionReferenceAudio> GetSectionReferenceAudioRepository()
        {
            return new AudioRepository<SectionReferenceAudio>(GetAudioPersistence<SectionReferenceAudio>());
        }

        public IAudioRepository<RetellBackTranslation> GetRetellBackTranslationRepository()
        {
            return new NotableAudioRepository<RetellBackTranslation>(
                audioRepository: GetAudioPersistence<RetellBackTranslation>(),
                conversationRepository: GetAudioRepository());
        }

        public IAudioRepository<SegmentBackTranslation> GetSegmentBackTranslationRepository()
        {
            return new NotableAudioRepository<SegmentBackTranslation>(
                audioRepository: GetAudioPersistence<SegmentBackTranslation>(),
                conversationRepository: GetAudioRepository());
        }

        public IAudioRepository<Draft> GetDraftRepository()
        {
            return new NotableAudioRepository<Draft>(
                audioRepository: GetAudioPersistence<Draft>(),
                conversationRepository: GetAudioRepository());
        }

        public IAudioRepository<Audio> GetAudioRepository()
        {
            return new AudioRepository<Audio>(GetAudioPersistence<Audio>());
        }

        public IAudioRepository<NotableAudio> GetNotableAudioRepository()
        {
            return new NotableAudioRepository<NotableAudio>(
                audioRepository: GetAudioPersistence<NotableAudio>(),
                conversationRepository: GetAudioRepository());
        }

        public IAudioRepository<StandardQuestion> GetStandardQuestionAudioRepository()
        {
            return new NotableAudioRepository<StandardQuestion>(GetAudioPersistence<StandardQuestion>(), GetAudioRepository());
        }

        public IAudioRepository<CommunityRetell> GetCommunityRetellAudioRepository()
        {
            return new NotableAudioRepository<CommunityRetell>(GetAudioPersistence<CommunityRetell>(), GetAudioRepository());
        }

        public IAudioRepository<Audio> GetTemporaryAudioRepository()
        {
            return new AudioRepository<Audio>(new CouchbaseLocalAudio<Audio>(LocalDatabase));
        }

        public IAudioPlayer GetAudioPlayer()
        {
            return Locator.Current.GetService<IAudioPlayer>();
        }

        [Obsolete("Use GetAudioPlayerService(Action stopAudioActivityCommand) method.", error: true)]
        public IAudioPlayerService GetAudioPlayerService()
        {
            return new AudioPlayerService(Locator.Current.GetService<IAudioPlayer>());
        }

        public IAudioPlayerService GetAudioPlayerService(Action stopAudioActivityCommand)
        {
            return new AudioPlayerService(
                simpleAudioPlayer: Locator.Current.GetService<IAudioPlayer>(),
                audioActivityService: GetAudioActivityService(),
                stopAudioActivityCommand: stopAudioActivityCommand);
        }

        public IPasswordService GetPasswordService()
        {
            return new PasswordService();
        }

        public IDataPersistence<T> GetPersistence<T>() where T : DomainEntity
        {
            return new CouchbaseLocal<T>(GetDatabase<T>(new BucketMapper()));
        }

        public IDataPersistence<T> GetAudioPersistence<T>() where T : Audio
        {
            return new CouchbaseLocalAudio<T>(GetDatabase<T>(new BucketMapper()));
        }

        public ViewModelContextProvider()
        {
            EssentialsWrapper = GetEssentialsWrapper();

            DateTimeWrapper = new DateTimeWrapper();
            RenderDatabase = new DatabaseWrapper(GetLogger(typeof(DatabaseWrapper)), Buckets.render.ToString(), LocalFolderPath);
            RenderAudioDatabase = new DatabaseWrapper(GetLogger(typeof(DatabaseWrapper)), Buckets.renderaudio.ToString(), LocalFolderPath);
            LocalDatabase = new DatabaseWrapper(GetLogger(typeof(DatabaseWrapper)), Buckets.localonlydata.ToString(), LocalFolderPath);

            var appSettings = Locator.Current.GetService<IAppSettings>();
            var syncServiceLogger = GetLogger(typeof(WebSyncService));

            WebSyncService = new WebSyncService(
                appSettings,
                syncServiceLogger,
                GetUserRepository(),
                new ReplicatorWrapper(syncServiceLogger, LocalFolderPath),
                new ReplicatorWrapper(syncServiceLogger, LocalFolderPath),
                new ReplicatorWrapper(syncServiceLogger, LocalFolderPath),
                EssentialsWrapper, DateTimeWrapper);

            var localSyncServiceLogger = GetLogger(typeof(LocalSyncService));

            var localReplicationService = new LocalReplicationService(appSettings,
                new LocalReplicator(localSyncServiceLogger, LocalFolderPath),
                new LocalReplicator(localSyncServiceLogger, LocalFolderPath),
                new LocalReplicator(localSyncServiceLogger, LocalFolderPath));

            SyncGatewayApiWrapper = new SyncGatewayApiWrapper(Locator.Current.GetService<IAppSettings>(), new HttpClient(), GetLogger(typeof(SyncGatewayApiWrapper)));

            ConnectivityService = new ConnectivityService(SyncGatewayApiWrapper);

            LocalSyncService = new LocalSyncService(
                GetHandshakeService(), localReplicationService, localSyncServiceLogger);

            WorkflowService = new WorkflowService(
                GetPersistence<WorkflowStatus>(),
                GetWorkflowRepository(),
                GetLogger(typeof(WorkflowService)));

            StageService = new StageService(WorkflowService, GetSectionRepository(), GetInterpretationService());

            SectionMovementService = new SectionMovementService(
                WorkflowService,
                StageService,
                GetSectionRepository(),
                GetSnapshotService(),
                GetCommunityTestService(),
                GetInterpretationService());

            GrandCentralStation = new GrandCentralStation(
                WorkflowService,
                StageService,
                SectionMovementService,
                GetSnapshotService());

            SessionStateService = new SessionStateService(
                new SessionStateRepository(
                    GetPersistence<UserProjectSession>()),
                GetTemporaryAudioRepository());

            SequencerFactory = new SequencerFactory();
        }

        public ViewModelContextProvider(IGrandCentralStation grandCentralStation)
        {
            GrandCentralStation = grandCentralStation;
        }

        public IUserMembershipService GetUserMembershipService()
        {
            return new UserMembershipService();
        }

        public void SetLoggedInUser(IUser user)
        {
            string username;
            if (user is RenderUser renderUser)
            {
                username = $"{renderUser.Username}:{renderUser.ProjectId}";
            }
            else
            {
                username = user.Username;
            }

            GrandCentralStation.ResetWorkForUser();

            GetLocalizationService().SetLocalization(user.LocaleId);

            AppCenter.SetUserId(username);
            LoggedInUser = user;
        }

        public void ClearLoggedInUser()
        {
            LoggedInUser = null;
        }

        public DeviceIdiom GetCurrentDeviceIdiom()
        {
            return DeviceInfo.Idiom;
        }

        public IModalService GetModalService()
        {
            return Splat.Locator.Current.GetService(typeof(IModalService)) as IModalService;
        }

        public IMiniAudioRecorderViewModel GetMiniAudioRecorderViewModel(Audio audio)
        {
            return new MiniAudioRecorderViewModel(audio, this);
        }

        public ITitleBarViewModel GetTitleBarViewModel(List<TitleBarElements> elementsToActivate,
            TitleBarMenuViewModel titleBarMenuViewModel,
            IViewModelContextProvider viewModelContextProvider,
            string pageTitle,
            Audio sectionTitleAudio,
            int sectionNumber,
            string secondaryPageTitle = "",
            string passageNumber = "")
        {
            return new TitleBarViewModel(
                elementsToActivate,
                titleBarMenuViewModel,
                this,
                pageTitle,
                sectionTitleAudio,
                sectionNumber,
                secondaryPageTitle,
                passageNumber);
        }

        public IBarPlayerViewModel GetBarPlayerViewModel(Audio audio, ActionState actionState, string title,
            int barPlayerPosition,
            TimeMarkers timeMarkers = null, List<TimeMarkers> passageMarkers = null,
            ImageSource secondaryButtonIcon = null, ReactiveCommand<Unit, IRoutableViewModel> secondaryButtonClickCommand = null,
            IObservable<bool> canPlayAudio = null,
            string glyph = null)
        {
            return new BarPlayerViewModel(audio, this, actionState, title, barPlayerPosition, timeMarkers,
                passageMarkers, secondaryButtonIcon, secondaryButtonClickCommand, canPlayAudio, glyph);
        }

        public IBarPlayerViewModel GetBarPlayerViewModel(Media media, ActionState actionState, string title,
            TimeMarkers timeMarkers = null, IObservable<bool> canPlayAudio = null)
        {
            return new BarPlayerViewModel(media, this, actionState, title, timeMarkers, canPlayAudio: canPlayAudio);
        }

        public IBarPlayerViewModel GetBarPlayerViewModel(AudioPlayback audioPlayback,
            ActionState actionState,
            string title,
            int barPlayerPosition,
            TimeMarkers timeMarkers = null,
            List<TimeMarkers> passageMarkers = null,
            ImageSource secondaryButtonIcon = null,
            ReactiveCommand<Unit, IRoutableViewModel> secondaryButtonClickCommand = null,
            IObservable<bool> canPlayAudio = null,
            string glyph = null)
        {
            return new BarPlayerViewModel(audioPlayback, this, actionState, title, barPlayerPosition, timeMarkers,
                passageMarkers, secondaryButtonIcon, secondaryButtonClickCommand, canPlayAudio, glyph);
        }

        public IDivisionPlayerViewModel GetDivisionPlayerViewModel(
            SectionReferenceAudio reference,
            List<PassageReference> passageReferences,
            TimeMarkers fullPassageTimeMarker,
            PassageNumber passageNumber)
        {
            return new DivisionPlayerViewModel(
                this,
                reference,
                passageNumber,
                fullPassageTimeMarker);
        }

        public IMiniWaveformPlayerViewModel GetMiniWaveformPlayerViewModel(
            Audio audio,
            ActionState actionState,
            string title,
            TimeMarkers timeMarkers = null,
            List<TimeMarkers> passageMarkers = null,
            ImageSource secondaryButtonIcon = null,
            ReactiveCommand<Unit, IRoutableViewModel> secondaryButtonClickCommand = null,
            bool showSecondaryButton = true,
            string glyph = null)
        {
            return new MiniWaveformPlayerViewModel(
                audio,
                this,
                actionState,
                title,
                timeMarkers,
                passageMarkers,
                secondaryButtonIcon,
                secondaryButtonClickCommand,
                showSecondaryButton,
                glyph);
        }

        public IMiniWaveformPlayerViewModel GetMiniWaveformPlayerViewModel(AudioPlayback audio,
            ActionState actionState,
            string title,
            TimeMarkers timeMarkers = null,
            List<TimeMarkers> passageMarkers = null,
            ImageSource secondaryButtonIcon = null,
            ReactiveCommand<Unit, IRoutableViewModel> secondaryButtonClickCommand = null,
            bool showSecondaryButton = true,
            string glyph = null)
        {
            return new MiniWaveformPlayerViewModel(audio, this, actionState, title, timeMarkers,
                passageMarkers, secondaryButtonIcon, secondaryButtonClickCommand, showSecondaryButton, glyph);
        }

        public IAudioEncodingService GetAudioEncodingService()
        {
            return new AudioEncodingService(GetLogger(typeof(AudioEncodingService)));
        }

        public string GetCacheDirectory()
        {
            return FileSystem.CacheDirectory;
        }

        public IDraftSelectionViewModel GetDraftSelectionViewModel(IBarPlayerViewModel miniWaveformPlayerViewModel,
            ActionState actionState)
        {
            return new DraftSelectionViewModel(miniWaveformPlayerViewModel, this, actionState);
        }

        public IUserRepository GetUserRepository()
        {
            return new UserRepository(GetPersistence<User>(), GetPersistence<RenderUser>());
        }

        public IEntityChangeListenerService GetUserChangeListenerService(List<Guid> userIds)
        {
            return new UserChangeListenerService(GetRenderChangeMonitoringService(), userIds);
        }

        public IAuthenticationApiWrapper GetAuthenticationApiWrapper()
        {
            var appSettings = Locator.Current.GetService<IAppSettings>();
            return new AuthenticationApiWrapper(new HttpClient(), appSettings.ApiEndpoint, appSettings.MaxAuthenticationAttempts);
        }

        public IAppCenterApiWrapper GetAppCenterApiWrapper()
        {
            var appSettings = Locator.Current.GetService<IAppSettings>();
            return new AppCenterApiWrapper(new HttpClient(), appSettings.AppCenterAppName, appSettings.AppCenterAPIToken);
        }

        public IBreathPauseAnalyzer GetBreathPauseAnalyzer()
        {
            return new StreamBreathPauseAnalyzer(Locator.Current.GetService<IAudioPlayer>());
        }

        public IWorkflowRepository GetWorkflowRepository()
        {
            return new WorkflowRepository(GetPersistence<RenderWorkflow>());
        }

        public ISessionStateService GetSessionStateService()
        {
            return SessionStateService;
        }

        public ISnapshotRepository GetSnapshotRepository()
        {
            return new SnapshotRepository(GetPersistence<Snapshot>(), GetSectionRepository(), GetDraftRepository());
        }

        public ICommunityTestService GetCommunityTestService() => new CommunityTestService(GetCommunityTestRepository(), GetSectionRepository());

        public ICommunityTestRepository GetCommunityTestRepository()
        {
            return new CommunityTestRepository(GetPersistence<CommunityTest>(),
                GetCommunityRetellAudioRepository(),
                GetNotableAudioRepository(),
                GetStandardQuestionAudioRepository(),
                GetResponseRepository());
        }

        public IAudioRepository<CommunityRetell> GetCommunityRetellRepository()
        {
            return new NotableAudioRepository<CommunityRetell>(GetAudioPersistence<CommunityRetell>(), GetAudioRepository());
        }

        public IAudioRepository<Response> GetResponseRepository()
        {
            return new NotableAudioRepository<Response>(GetAudioPersistence<Response>(), GetAudioRepository());
        }

        public IMachineLoginStateRepository GetMachineLoginStateRepository()
        {
            return new MachineLoginStateRepository(GetPersistence<MachineLoginState>());
        }

        public ICloseApplication GetCloseApplication()
        {
            return Splat.Locator.Current.GetService(typeof(ICloseApplication)) as ICloseApplication;
        }

        public IDownloadService GetDownloadService()
        {
            return Splat.Locator.Current.GetService(typeof(IDownloadService)) as IDownloadService;
        }

        public ILocalizationService GetLocalizationService()
        {
            return Splat.Locator.Current.GetService(typeof(ILocalizationService)) as ILocalizationService;
        }

        public IFileNameGeneratorService GetFileNameGeneratorService()
        {
            return new FileNameGeneratorService();
        }

        public IOffloadService GetOffloadService()
        {
            return new OffloadService(
                GetPersistence<Project>(),
                GetLocalProjectsRepository(),
                new OffloadRepository(LocalDatabase, RenderDatabase, RenderAudioDatabase),
                GetSessionStateService(),
                GetAudioIntegrityService(),
                GetLogger(typeof(OffloadService)));
        }

        public IDocumentChangeListener GetRenderChangeMonitoringService()
        {
            return new DocumentChangeListener(GetLogger(typeof(DocumentChangeListener)), Buckets.render.ToString(), LocalFolderPath);
        }

        public IAudioActivityService GetAudioActivityService()
        {
            return Splat.Locator.Current.GetService(typeof(IAudioActivityService)) as IAudioActivityService;
        }

        public IAudioRecorderServiceWrapper GetAudioRecorderService(
            Func<Task> stopAudioActivityCommand,
            Func<Task> onRecordFailedCommand,
            Func<Task> onRecordDeviceRestoreCommand)
        {
            return new AudioRecorderServiceWrapper(
                audioActivityService: GetAudioActivityService(),
                recorderFactory: GetAudioRecorderFactory(),
                stopRecordingCommand: stopAudioActivityCommand,
                onRecordFailedCommand: onRecordFailedCommand,
                onRecordDeviceRestoreCommand: onRecordDeviceRestoreCommand);
        }

        public ITextMeter GetTextMeter()
        {
            return Splat.Locator.Current.GetService(typeof(ITextMeter)) as ITextMeter;
        }

        public ILocalProjectDownloader GetLocalProjectDownloader(Guid projectId)
        {
            return new LocalProjectDownloader(Locator.Current.GetService<IAppSettings>(),
                projectId,
                GetLogger(typeof(LocalProjectDownloader)),
                GetPersistence<Project>(),
                GetPersistence<WorkflowStatus>(),
                GetEssentialsWrapper(),
                LocalFolderPath);
        }

        public IOneShotReplicator GetOneShotReplicator(List<Guid> projectChannels, List<Guid> userChannels,
            string syncGatewayUsername = null, string syncGatewayPassword = null)
        {
            syncGatewayUsername = syncGatewayUsername ?? WebSyncService.SyncGatewayUsername;
            syncGatewayPassword = syncGatewayPassword ?? WebSyncService.SyncGatewayPassword;
            var replicator = new OneShotReplicator(
                logger: GetLogger(typeof(WebSyncService)),
                connectionString: WebSyncService.ConnectionString,
                maxSyncAttempts: WebSyncService.MaxSyncAttempts,
                syncGatewayUsername: syncGatewayUsername,
                syncGatewayPassword: syncGatewayPassword,
                projectChannels:
                projectChannels,
                userChannels: userChannels,
                databasePath: LocalFolderPath,
                GetPersistence<Project>(), GetPersistence<WorkflowStatus>(), GetEssentialsWrapper());

            return replicator;
        }

        public IRenderLogger GetLogger(Type loggerType) => new TypedRenderLogger(loggerType);

        public IMenuPopupService GetMenuPopupService() => Locator.Current.GetService<IMenuPopupService>();

        public IWaveFormService GetWaveFormService() => new WaveFormService(GetLogger(typeof(WaveFormService)));

        public ITempAudioService GetTempAudioService(Audio audio, string audioPath = null, bool mutable = false)
            => new TempAudioService(audio, audioPath, mutable, GetAppDirectory(), GetAudioEncodingService());

        public ITempAudioService GetTempAudioService(AudioPlayback audio)
            => new TempAudioService(audio, GetAppDirectory(), GetAudioEncodingService());

        public IAppDirectory GetAppDirectory()
        {
            return Locator.Current.GetService<IAppDirectory>();
        }

        public IAppSettings GetAppSettings()
        {
            return Locator.Current.GetService<IAppSettings>();
        }

        public ISequencerFactory GetSequencerFactory()
        {
            return SequencerFactory;
        }

        public IProjectDownloadService GetProjectDownloaderService()
        {
            return Locator.Current.GetService(typeof(IProjectDownloadService)) as IProjectDownloadService;
        }

        public ILocalDatabaseReplicationManager GetLocalDatabaseReplicationManager()
        {
            return (new LocalDatabaseReplicationManager(GetDbBackupService(), GetLogger(typeof(LocalDatabaseReplicationManager)), LocalFolderPath));
        }

        public IAudioIntegrityService GetAudioIntegrityService()
        {
            return new AudioIntegrityService(RenderAudioDatabase, GetLogger(typeof(AudioIntegrityService)));
        }

        public IAudioLossRetryDownloadService GetAudioLossRetryDownloadService()
        {
            return new AudioLossRetryDownloadService(GetOffloadService(), GetAudioIntegrityService(), GetModalService());
        }

        public IEntityChangeListenerService GetDocumentSubscriptionManagerService()
        {
            var gcs = GetGrandCentralStation();
            return new WorkflowChangeListenerService(GetRenderChangeMonitoringService(), WorkflowService.ProjectWorkflow, WorkflowService.FullWorkflowStatusList);
        }

        public IUsbSyncFolderStorageService GetUsbSyncFolderStorageService()
        {
            return Locator.Current.GetService(typeof(IUsbSyncFolderStorageService)) as IUsbSyncFolderStorageService;
        }

        public ISyncManager GetSyncManager()
        {
            if (SyncManager is not null)
            {
                return SyncManager;
            }

            var usbSyncService = new UsbSyncService(
                GetLocalDatabaseReplicationManager(),
                GetDownloadService(),
                GetUsbSyncFolderStorageService(),
                GetModalService(),
                GetPersistence<Project>());

            SyncManager = new SyncManager(
                WebSyncService,
                LocalSyncService,
                usbSyncService,
                GetSyncGatewayApiWrapper(),
                GetLocalProjectsRepository(),
                GetPersistence<Project>(),
                GetPersistence<RenderProjectStatistics>(),
                GetUserMachineSettingsRepository(),
                GetAuthenticationApiWrapper(),
                ConnectivityService,
                new SyncStateService(),
                GetLogger(typeof(SyncManager)));

            return SyncManager;
        }

        public IDbBackupService GetDbBackupService()
        {
            return Locator.Current.GetService(typeof(IDbBackupService)) as IDbBackupService;
        }

        public ISectionMovementService GetSectionMovementService()
        {
            return SectionMovementService;
        }

        public IWorkflowService GetWorkflowService()
        {
            return WorkflowService;
        }

        public IStageService GetStageService()
        {
            return StageService;
        }

        public IDeletedUserCleanService GetDeletedUserCleanService(Guid projectId)
        {
            return new DeletedUserCleanService(GetUserRepository(), GetWorkflowRepository(), projectId);
        }

        public ISnapshotService GetSnapshotService()
        {
            return new SnapshotService(
                GetSnapshotRepository(),
                WorkflowService,
                StageService,
                GetSectionRepository(),
                GetAudioRepository(),
                GetDraftRepository(),
                GetRetellBackTranslationRepository(),
                GetSegmentBackTranslationRepository(),
                GetUserRepository());
        }

        public IInterpretationService GetInterpretationService()
        {
            return new InterpretationService(WorkflowService, GetSectionRepository());
        }

        public IHandshakeService GetHandshakeService()
        {
            var handshakeServiceLogger = GetLogger(typeof(HandshakeService));
            return new HandshakeService(
                new NetworkConnectionService(handshakeServiceLogger),
                new BroadcastService(handshakeServiceLogger));
        }
    }
}