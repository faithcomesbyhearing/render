using System.Reactive;
using FluentAssertions;
using Moq;
using ReactiveUI;
using Render.Components.BarPlayer;
using Render.Components.MiniWaveformPlayer;
using Render.Kernel;
using Render.Models.Audio;
using Render.Models.LocalOnlyData;
using Render.Models.Sections;
using Render.Models.Snapshot;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Translator.DraftingPage;
using Render.Pages.Translator.DraftSelectPage;
using Render.Repositories.Audio;
using Render.Repositories.LocalDataRepositories;
using Render.Repositories.SectionRepository;
using Render.Repositories.SnapshotRepository;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Services.AudioPlugins.AudioPlayer;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;
using Render.Services.AudioServices;
using Render.Services.SessionStateServices;
using Render.UnitTests.App.Kernel;
using Draft = Render.Models.Sections.Draft;
using Section = Render.Models.Sections.Section;

namespace Render.UnitTests.App.Pages.Translator
{
    public class DraftingViewModelTests : ViewModelTestBase
    {
        private readonly Section _section;
        private readonly Step _step;
        private readonly Passage _passage;
        private readonly Stage _stage;
        
        private readonly Mock<ISessionStateService> _mockSessionService;
        private readonly Mock<ISnapshotRepository> _mockSnapshotRepository = new();
        private readonly Mock<IUserMachineSettingsRepository> _mockUserMachineSettingsRepository = new();

        public DraftingViewModelTests()
        {
            _section = Section.UnitTestEmptySection();
            _step = new Step(RenderStepTypes.Draft, Roles.Drafting);
            _step.StepSettings.SetSetting(SettingType.AllowEditing, true);
            _stage = new Stage();
            _stage.SetName("Stage");
            _section.Passages.First().ChangeCurrentDraftAudio(new Draft(Guid.Empty, Guid.Empty, Guid.Empty));
            _passage = _section.Passages.First();
            _passage.CurrentDraftAudio.SetAudio(new byte[] { 0, 1, 2, 3, 4, 5, 6 });
            
            var mockSectionRepository = new Mock<ISectionRepository>();
            mockSectionRepository.Setup(x => x.GetSectionAsync(It.IsAny<Guid>(), It.IsAny<bool>()))
                .ReturnsAsync(_section);
            
            var mockPreviousDraftViewModel = new Mock<IMiniWaveformPlayerViewModel>();
            mockPreviousDraftViewModel.Setup(x => x.AudioPlayerState).Returns(AudioPlayerState.Paused);
            
            var mockAudioEncodingService = new Mock<IAudioEncodingService>();

            var mockSequencerFactory = new Mock<ISequencerFactory>();
            mockSequencerFactory.Setup(x => x.CreateRecorder(
                It.IsAny<Func<IAudioPlayer>>(),
                It.IsAny<Func<IAudioRecorder>>(),
                It.IsAny<FlagType>()
            )).Returns(new Mock<ISequencerRecorderViewModel>().Object);
            MockContextProvider.Setup(x => x.GetSequencerFactory()).Returns(mockSequencerFactory.Object);
            
            MockContextProvider.Setup(x => x.GetAudioEncodingService()).Returns(mockAudioEncodingService.Object);
            MockContextProvider.Setup(x => x.GetSectionRepository()).Returns(mockSectionRepository.Object);
            MockContextProvider.Setup(x => x.GetBarPlayerViewModel(
                    It.IsAny<Audio>(),
                    It.IsAny<ActionState>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    null,
                    null,
                    It.IsAny<ImageSource>(),
                    It.IsAny<ReactiveCommand<Unit, IRoutableViewModel>>(),
                    It.IsAny<IObservable<bool>>(),
                    It.IsAny<string>()))
                .Returns(new Mock<IBarPlayerViewModel>().Object);
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
                .Returns(mockPreviousDraftViewModel.Object);
            MockContextProvider.Setup(x => x.GetCacheDirectory()).Returns("Path");
            
            _mockSessionService = new Mock<ISessionStateService>();
            _mockSessionService.Setup(x => x.AudioIds).Returns(new List<Guid>());
            MockContextProvider.Setup(x => x.GetSessionStateService()).Returns(_mockSessionService.Object);
            _mockSessionService.Setup(x => x.ActiveSession)
                .Returns(new UserProjectSession(Guid.Empty, Guid.Empty, Guid.Empty));
            
            MockContextProvider.Setup(x => x.GetUserMachineSettingsRepository())
                .Returns(_mockUserMachineSettingsRepository.Object);
            _mockUserMachineSettingsRepository.Setup(x => x.GetUserMachineSettingsForUserAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new UserMachineSettings(Guid.Empty));

            MockContextProvider.Setup(x => x.GetLoggedInUser()).Returns(new RenderUser("", Guid.Empty));
            MockContextProvider.Setup(x => x.GetSnapshotRepository()).Returns(_mockSnapshotRepository.Object);
            _mockSnapshotRepository.Setup(x => x.GetSnapshotsForSectionAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Snapshot>());
        }

        //Make a test that mocks the audio repository to return a new draft with audio
        //Test of the list is different then original draft.
        //Mock DraftVM before LoadSession method is called.
        //Mock audio repo to return new audio.
        [Fact]
        public async Task LoadDraftFromSession_Should_UpdateDraftListWithNewAudio()
        {
            //Arrange
            var newAudio = new Audio(Guid.Empty, Guid.Empty, Guid.Empty);
            newAudio.SetAudio(new byte[] { 0, 1, 2, 3, 4, 5, 6 });
            var mockAudioRepo = new Mock<IAudioRepository<Audio>>();
            mockAudioRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(newAudio);
            MockContextProvider.Setup(x => x.GetTemporaryAudioRepository()).Returns(mockAudioRepo.Object);

            var userSession = new UserProjectSession(Guid.Empty, Guid.Empty, Guid.Empty);
            userSession.SetPassageNumber(new PassageNumber(1));
            userSession.StartStep(_section.Id, "Drafting");
            _mockSessionService.Setup(x => x.ActiveSession)
                .Returns(userSession);
            
            var audioList = new List<Guid>() { new(), new() };
            _mockSessionService.Setup(x => x.AudioIds).Returns(audioList);
            MockContextProvider.Setup(x => x.GetSessionStateService()).Returns(_mockSessionService.Object);
            
            var vm = await DraftingViewModel.CreateAsync(_section, _passage, _step, MockContextProvider.Object, _stage);
          
            for(int i = 2; i < vm.DraftViewModels.Count; ++i)
            {
                var draftVm = vm.DraftViewModels[i];
                if (draftVm.Audio.Data.Length == 0)
                {
                    var audio = new Audio(default, default, default);
                    audio.SetAudio(new byte[]{1,3,2,4,5,6,4,3,2});
                    draftVm.SetAudio(audio);
                }
            }
            
            //Assert
            vm.DraftViewModels.Any(x => x.Audio.Data.Length == newAudio.Data.Length).Should().BeTrue();
        }
        
        [Fact]
        public async Task ViewModelCreation_Succeeds()
        {
            //Arrange
            //Act
            var vm = await DraftingViewModel.CreateAsync(_section, _passage, _step, MockContextProvider.Object, _stage);
            
            //Assert
            vm.ActionViewModelBaseSourceList.Count.Should().Be(1);
        }
        
        [Fact]
        public async Task ViewModelCreation_Calls_RemoveDraftAudios()
        {
            //Arrange
            await DraftingViewModel.CreateAsync(_section, _passage, _step, MockContextProvider.Object, _stage, false);

            //Act & Assert
            _mockSessionService.Verify(x => x.RemoveDraftAudios(It.IsAny<IEnumerable<Guid>>()), Times.Once);
        }

        [Fact]
        public async Task NavigateToDraftSelect_Succeeds()
        {
            //Arrange
            var mockTempAudioRepo = new Mock<IAudioRepository<Audio>>();
            MockContextProvider.Setup(x => x.GetTemporaryAudioRepository()).Returns(mockTempAudioRepo.Object);
            var vm = await DraftingViewModel.CreateAsync(_section, _passage, _step, MockContextProvider.Object, _stage);
            SetupViewModelForNavigationTest(vm);
            
            //Act & Assert
            await VerifyNavigationResultAsync<DraftSelectViewModel>(vm.ProceedButtonViewModel.NavigateToPageCommand);
        }
    }
}