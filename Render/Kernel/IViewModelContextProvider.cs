using Render.WebAuthentication;
using Render.Interfaces.AudioServices;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Audio;
using Render.Models.Sections;
using Render.Models.Sections.CommunityCheck;
using Render.Models.Users;
using Render.Repositories.Audio;
using Render.Repositories.Kernel;
using Render.Repositories.LocalDataRepositories;
using Render.Repositories.SectionRepository;
using Render.Repositories.SnapshotRepository;
using Render.Repositories.UserRepositories;
using Render.Repositories.WorkflowRepositories;
using Render.Services;
using Render.Services.AudioServices;
using Render.Services.FileNaming;
using Render.Services.SessionStateServices;
using Render.Services.SyncService;
using Render.TempFromVessel.Kernel;
using Render.Interfaces.WrappersAndExtensions;
using Render.Interfaces.EssentialsWrappers;
using Render.Components.TitleBar;
using Render.Services.UserServices;
using Render.Components.AudioRecorder;
using ReactiveUI;
using System.Reactive;
using Render.Components.BarPlayer;
using Render.Components.MiniWaveformPlayer;
using Render.Components.DraftSelection;
using Render.Services.PasswordServices;
using Render.Interfaces;
using Render.Components.DivisionPlayer;
using Render.Kernel.SyncServices;
using Render.Services.WaveformService;
using Render.Sequencer.Contracts.Interfaces;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;
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
    public interface IViewModelContextProvider
    {
        string LocalFolderPath { get; }
        
        IEntityChangeListenerService UserChangeListenerService { get; set; }

        Task CompactDatabasesAsync();

        Page CreateLaunchPage(RoutingState routingState);
        
        ISyncGatewayApiWrapper GetSyncGatewayApiWrapper();

        IGrandCentralStation GetGrandCentralStation();

        IUser GetLoggedInUser();

        IDataPersistence<T> GetPersistence<T>() where T : DomainEntity;

        IDataPersistence<T> GetAudioPersistence<T>() where T : Audio;

        IUserMembershipService GetUserMembershipService();

        void SetLoggedInUser(IUser user);

        void ClearLoggedInUser();

        IEssentialsWrapper GetEssentials();

        IUserMachineSettingsRepository GetUserMachineSettingsRepository();

        IAudioRecorderServiceWrapper GetAudioRecorderService(
            Func<Task> stopAudioActivityCommand,
            Func<Task> onMediaCaptureFailedCommand,
            Func<Task> onMediaCaptureDeviceRestoreCommand);

        ILocalProjectsRepository GetLocalProjectsRepository();

        ISectionRepository GetSectionRepository();

        IAudioRepository<SectionReferenceAudio> GetSectionReferenceAudioRepository();

        IAudioRepository<RetellBackTranslation> GetRetellBackTranslationRepository();

        IAudioRepository<SegmentBackTranslation> GetSegmentBackTranslationRepository();

        IAudioRepository<Draft> GetDraftRepository();

        IAudioRepository<Audio> GetAudioRepository();

        IAudioRepository<NotableAudio> GetNotableAudioRepository();

        IAudioRepository<Audio> GetTemporaryAudioRepository();

        IAudioRepository<StandardQuestion> GetStandardQuestionAudioRepository();

        IAudioPlayer GetAudioPlayer();

        [Obsolete("Use GetAudioPlayerService(Action stopAudioActivityCommand) method.", error: true)]
        IAudioPlayerService GetAudioPlayerService();

        IAudioPlayerService GetAudioPlayerService(Action stopAudioActivityCommand);
        
        IPasswordService GetPasswordService();

        DeviceIdiom GetCurrentDeviceIdiom();

        IModalService GetModalService();

        IMiniAudioRecorderViewModel GetMiniAudioRecorderViewModel(Audio audio);

        ITitleBarViewModel GetTitleBarViewModel(
            List<TitleBarElements> elementsToActivate,
            TitleBarMenuViewModel titleBarMenuViewModel,
            IViewModelContextProvider viewModelContextProvider,
            string pageTitle,
            Audio sectionTitleAudio,
            int sectionNumber,
            string secondaryPageTitle = "",
            string passageNumber = "");

        IBarPlayerViewModel GetBarPlayerViewModel(Audio audio,
            ActionState actionState,
            string title,
            int barPlayerPosition,
            TimeMarkers timeMarkers = null,
            List<TimeMarkers> passageMarkers = null,
            ImageSource secondaryButtonIcon = null,
            ReactiveCommand<Unit, IRoutableViewModel> secondaryButtonClickCommand = null,
            IObservable<bool> canPlayAudio = null,
            string glyph = null);

        IBarPlayerViewModel GetBarPlayerViewModel(
            Media media, 
            ActionState actionState, 
            string title,
            TimeMarkers timeMarkers = null,
            IObservable<bool> canPlayAudio = null);

        IBarPlayerViewModel GetBarPlayerViewModel(AudioPlayback audioPlayback,
            ActionState actionState,
            string title,
            int barPlayerPosition,
            TimeMarkers timeMarkers = null,
            List<TimeMarkers> passageMarkers = null,
            ImageSource secondaryButtonIcon = null,
            ReactiveCommand<Unit, IRoutableViewModel> secondaryButtonClickCommand = null,
            IObservable<bool> canPlayAudio = null,
            string glyph = null);

        IDivisionPlayerViewModel GetDivisionPlayerViewModel(
            SectionReferenceAudio reference,
            List<PassageReference> passageReferences,
            TimeMarkers fullPassageTimeMarker,
            PassageNumber passageNumber);
        IMiniWaveformPlayerViewModel GetMiniWaveformPlayerViewModel(
            Audio audio,
            ActionState actionState,
            string title,
            TimeMarkers timeMarkers = null,
            List<TimeMarkers> passageMarkers = null,
            ImageSource secondaryButtonIcon = null,
            ReactiveCommand<Unit, IRoutableViewModel> secondaryButtonClickCommand = null,
            bool showSecondaryButton = true,
            string glyph = null);

        IMiniWaveformPlayerViewModel GetMiniWaveformPlayerViewModel(AudioPlayback audio,
            ActionState actionState,
            string title,
            TimeMarkers timeMarkers = null,
            List<TimeMarkers> passageMarkers = null,
            ImageSource secondaryButtonIcon = null,
            ReactiveCommand<Unit, IRoutableViewModel> secondaryButtonClickCommand = null,
            bool showSecondaryButton = true,
            string glyph = null);

        IAudioEncodingService GetAudioEncodingService();

        string GetCacheDirectory();

        IDraftSelectionViewModel GetDraftSelectionViewModel(
            IBarPlayerViewModel miniWaveformPlayerViewModel,
            ActionState actionState);

        IUserRepository GetUserRepository();
        
        IEntityChangeListenerService GetUserChangeListenerService(List<Guid> userIds);

        IAuthenticationApiWrapper GetAuthenticationApiWrapper();

        IAppCenterApiWrapper GetAppCenterApiWrapper();

        IBreathPauseAnalyzer GetBreathPauseAnalyzer();

        IWorkflowRepository GetWorkflowRepository();

        ISessionStateService GetSessionStateService();

        ICommunityTestService GetCommunityTestService();

        ICommunityTestRepository GetCommunityTestRepository();

        ISnapshotRepository GetSnapshotRepository();

        IMachineLoginStateRepository GetMachineLoginStateRepository();

        IDownloadService GetDownloadService();

        IFileNameGeneratorService GetFileNameGeneratorService();

        IAudioRepository<CommunityRetell> GetCommunityRetellRepository();

        IAudioRepository<Response> GetResponseRepository();
        
        IOffloadService GetOffloadService();

        IDocumentChangeListener GetRenderChangeMonitoringService();

        ILocalizationService GetLocalizationService();

        ITextMeter GetTextMeter();

        ILocalProjectDownloader GetLocalProjectDownloader(Guid projectId);

        IOneShotReplicator GetOneShotReplicator(
            List<Guid> projectChannels,
            List<Guid> userChannels,
            string syncGatewayUsername = null,
            string syncGatewayPassword = null);

        IAudioActivityService GetAudioActivityService();

        ICloseApplication GetCloseApplication();

        IRenderLogger GetLogger(Type loggerType);

        IMenuPopupService GetMenuPopupService();

        IWaveFormService GetWaveFormService();

        /// <summary>
        /// Creates TempAudioService.
        /// </summary>
        /// <param name="audio">audio to process</param>
        /// <param name="audioPath">path to audio, set if path for audio already exists</param>
        /// <param name="mutable">set 'true' if audio data changes should be tracked</param>
        ITempAudioService GetTempAudioService(Audio audio, string audioPath = null, bool mutable = false);

        public ITempAudioService GetTempAudioService(AudioPlayback audio);
        
        IAppDirectory GetAppDirectory();

        IAppSettings GetAppSettings();

        ISequencerFactory GetSequencerFactory();

        Func<int, IAudioRecorder> GetAudioRecorderFactory();

        IProjectDownloadService GetProjectDownloaderService();

        ILocalDatabaseReplicationManager GetLocalDatabaseReplicationManager();
        
        IAudioIntegrityService GetAudioIntegrityService();
        
        IAudioLossRetryDownloadService GetAudioLossRetryDownloadService();
        
        IEntityChangeListenerService GetDocumentSubscriptionManagerService();

        IUsbSyncFolderStorageService GetUsbSyncFolderStorageService();
        
        ISyncManager GetSyncManager();

        IDbBackupService GetDbBackupService();

        ISnapshotService GetSnapshotService();
        
        ISectionMovementService GetSectionMovementService();

        IWorkflowService GetWorkflowService();

        IStageService GetStageService();

        IDeletedUserCleanService GetDeletedUserCleanService(Guid projectId);

        IInterpretationService GetInterpretationService();

        IHandshakeService GetHandshakeService();
    }
}