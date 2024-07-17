using Moq;
using Render.Components.DraftSelection;
using Render.Components.MiniWaveformPlayer;
using Render.Kernel;
using Render.Models.Audio;
using Render.Models.LocalOnlyData;
using Render.Models.Project;
using Render.Models.Scope;
using Render.Models.Sections;
using Render.Models.Snapshot;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Translator.DraftingPage;
using Render.Pages.Translator.DraftSelectPage;
using Render.Pages.Translator.PassageListen;
using Render.Repositories.Audio;
using Render.Repositories.Kernel;
using Render.Repositories.SectionRepository;
using Render.Repositories.SnapshotRepository;
using Render.Repositories.UserRepositories;
using Render.Repositories.WorkflowRepositories;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Services.AudioPlugins.AudioPlayer;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;
using Render.Services.AudioServices;
using Render.Services.SessionStateServices;
using Render.TempFromVessel.Project;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Translator
{
    public class DraftSelectViewModelTests : ViewModelTestBase
    {
        private readonly Section _section;
        private readonly Step _step;
        private readonly DraftViewModel _draft;
        private readonly Passage _passage;
        private readonly Stage _stage;
        
        private readonly RenderWorkflow _renderWorkflow = RenderWorkflow.Create(Guid.Empty);
        private readonly Section _section1 = Section.UnitTestEmptySection();
        private readonly Section _section2 = Section.UnitTestEmptySection();
        private readonly Guid _projectId = Guid.NewGuid();

        private readonly Mock<IWorkflowRepository> _mockWorkflowRepository = new();
        private readonly Mock<IUserRepository> _mockUserRepository = new();
        private readonly Mock<ISectionRepository> _mockSectionRepository = new();
        private readonly Mock<IDataPersistence<RenderProjectStatistics>> _mockStatisticsRepository = new();
        private readonly Mock<ISnapshotRepository> _mockSnapshotRepository = new();

        public DraftSelectViewModelTests()
        {
            _section = Section.UnitTestEmptySection();
            _section.Passages.Add(new Passage(new PassageNumber(4)));
            _passage = _section.Passages.First();
            _step = new Step(RenderStepTypes.Draft, Roles.Drafting);
            _stage = new Stage();
            _stage.SetName("Stage");     
            _draft = new DraftViewModel(1, MockContextProvider.Object);
            var audio = new Audio(Guid.Empty, Guid.Empty, Guid.Empty);
            audio.SetAudio(new byte[] {0, 42});
            _draft.SetAudio(audio);
            
            var mockDraftSelectionViewModel = new Mock<IDraftSelectionViewModel>();
            var mockMiniWaveformPlayerViewModel = new Mock<IMiniWaveformPlayerViewModel>();
            MockContextProvider.Setup(x => x.GetMiniWaveformPlayerViewModel(
                    It.IsAny<Audio>(),
                    It.IsAny<ActionState>(),
                    It.IsAny<string>(), 
                    It.IsAny<TimeMarkers>(),
                    null,
                    null,
                    null,
                    It.IsAny<bool>(),
                    It.IsAny<string>()))
                .Returns(new Mock<IMiniWaveformPlayerViewModel>().Object);
            
            MockContextProvider.Setup(x => x.GetDraftSelectionViewModel(
                    It.IsAny<IMiniWaveformPlayerViewModel>(), It.IsAny<ActionState>()))
                .Returns(mockDraftSelectionViewModel.Object);
            mockDraftSelectionViewModel.Setup(x => x.DraftSelectionState)
                .Returns(DraftSelectionState.Selected);
            mockDraftSelectionViewModel.Setup(x => x.MiniWaveformPlayerViewModel)
                .Returns(mockMiniWaveformPlayerViewModel.Object);
            mockMiniWaveformPlayerViewModel.Setup(x => x.AudioTitle).Returns(_draft.Title);

            MockContextProvider.Setup(x => x.GetDraftRepository())
                .Returns(new Mock<IAudioRepository<Models.Sections.Draft>>().Object);
            MockContextProvider.Setup(x => x.GetSectionRepository())
                .Returns(new Mock<ISectionRepository>().Object);
            
            var audioPlayerService = new Mock<IAudioPlayerService>();
            audioPlayerService.Setup(x => x.CurrentPosition).Returns(10);
            audioPlayerService.Setup(x => x.Duration).Returns(500);
            var array = new byte[5000];
            new Random().NextBytes(array);

            var mockSequencerFactory = new Mock<ISequencerFactory>();
            mockSequencerFactory.Setup(x => x.CreateRecorder(
                It.IsAny<Func<IAudioPlayer>>(),
                It.IsAny<Func<IAudioRecorder>>(),
                It.IsAny<FlagType>()
            )).Returns(new Mock<ISequencerRecorderViewModel>().Object);
            MockContextProvider.Setup(x => x.GetSequencerFactory()).Returns(mockSequencerFactory.Object);

            
            MockContextProvider.Setup(x => x.GetCacheDirectory()).Returns("cache");

            var mockSessionService = new Mock<ISessionStateService>();
            mockSessionService.Setup(x => x.AudioIds).Returns(new List<Guid>());
            mockSessionService.Setup(x => x.ActiveSession)
                .Returns(new UserProjectSession(Guid.Empty, Guid.Empty, Guid.Empty));
            MockContextProvider.Setup(x => x.GetSessionStateService()).Returns(mockSessionService.Object);
            MockContextProvider.Setup(x => x.GetSnapshotRepository())
                .Returns(_mockSnapshotRepository.Object);
            
            MockContextProvider.Setup(x => x.GetSectionRepository())
                .Returns(_mockSectionRepository.Object);

            _mockWorkflowRepository.Setup(x => x.GetWorkflowForProjectIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(_renderWorkflow);
            _mockSnapshotRepository.Setup(x => x.GetSnapshotsForSectionAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Snapshot>());
            _mockSectionRepository.Setup(x => x.GetSectionsForProjectAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Section>{_section1, _section2});

            MockContextProvider.Setup(x => x.GetWorkflowRepository())
                .Returns(_mockWorkflowRepository.Object);
            MockContextProvider.Setup(x => x.GetUserRepository())
                .Returns(_mockUserRepository.Object);

            MockContextProvider.Setup(x => x.GetPersistence<RenderProjectStatistics>())
                .Returns(_mockStatisticsRepository.Object);
            _mockStatisticsRepository
                .Setup(x => x.QueryOnFieldAsync(It.IsAny<string>(), It.IsAny<string>(), 
                    It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<RenderProjectStatistics>());
            var mockAudioRepository = new Mock<IAudioRepository<Audio>>();
            MockContextProvider.Setup(x => x.GetTemporaryAudioRepository())
                .Returns(mockAudioRepository.Object);
            
            //Home screen
            var projectRepositoryMock = new Mock<IDataPersistence<Project>>();
            var project = new Project("Project Name", string.Empty, string.Empty);
            projectRepositoryMock.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync(project);
            MockContextProvider.Setup(x => x.GetPersistence<Project>()).Returns(projectRepositoryMock.Object);
            var mockScopeRepo = new Mock<IDataPersistence<Scope>>();
            var scope = new Scope(_projectId);
            var defaultDate = DateTimeOffset.MinValue;
            scope.SetFirstSectionDraftedDate(defaultDate);
            scope.SetFinalSectionApprovedDate(defaultDate);
            mockScopeRepo.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync(scope);
            MockContextProvider.Setup(x => x.GetPersistence<Scope>()).Returns(mockScopeRepo.Object);
            _step.StepSettings.SetSetting(SettingType.DoPassageReview, false);
            _step.StepSettings.SetSetting(SettingType.DoSectionReview, false);
            _passage.CurrentDraftAudio.SetCreatedBy(It.IsAny<Guid>(), It.IsAny<string>());
            MockContextProvider.Setup(x => x.GetLoggedInUser())
                .Returns(new User("Navi", "navi"));
        }
        
        [Fact]
        public void NavigateForward_WhenPassageReviewOffAndMorePassagesAndPassageListenOn_Succeeds()
        {
            //Arrange
            _step.StepSettings.SetSetting(SettingType.DoPassageReview, false);
            _step.StepSettings.SetSetting(SettingType.DoPassageListen, true);
            var vm = new DraftSelectViewModel(_section, new List<DraftViewModel> {_draft}, 
                MockContextProvider.Object, _passage, _step, _stage);
            SetupViewModelForNavigationTest(vm);
            
            //Act & Assert
            VerifyNavigationResult<PassageListenViewModel>(vm.ProceedButtonViewModel.NavigateToPageCommand);
        }
        
        [Fact]
        public void NavigateForward_WhenPassageReviewOffAndMorePassagesAndPassageListenOff_Succeeds()
        {
            //Arrange
            _step.StepSettings.SetSetting(SettingType.DoPassageReview, false);
            _step.StepSettings.SetSetting(SettingType.DoPassageListen, false);
            var vm = new DraftSelectViewModel(_section, new List<DraftViewModel> {_draft}, 
                MockContextProvider.Object, _passage, _step, _stage);
            SetupViewModelForNavigationTest(vm);
            
            //Act & Assert
            VerifyNavigationResult<DraftingViewModel>(vm.ProceedButtonViewModel.NavigateToPageCommand);
        }
    }
}