using System.Reactive;
using FluentAssertions;
using Moq;
using ReactiveUI;
using Render.Components.BarPlayer;
using Render.Components.MiniWaveformPlayer;
using Render.Kernel;
using Render.Models.Audio;
using Render.Models.Sections;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Consultant.ConsultantApproval;
using Render.Repositories.SectionRepository;
using Render.Repositories.UserRepositories;
using Render.Repositories.WorkflowRepositories;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Services.AudioPlugins.AudioPlayer;
using Render.Services.AudioServices;
using Render.Services.SessionStateServices;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Consultant.ConsultantApproval
{
    public class SelectSectionToApproveViewModelTests : ViewModelTestBase
    {
        private readonly Section _section;
        private readonly Step _step;
        private readonly Stage _stage;

        private readonly Mock<IWorkflowRepository> _mockWorkflowRepository = new();
        private readonly Mock<IUserRepository> _mockUserRepository = new();
        private readonly Mock<ISectionRepository> _mockSectionRepository = new();
        private readonly RenderWorkflow _renderWorkflow = RenderWorkflow.Create(Guid.Empty);
        private readonly Section _section1 = Section.UnitTestEmptySection();
        private readonly Section _section2 = Section.UnitTestEmptySection();
        private readonly RenderUser _user1 = new("User1", Guid.Empty);
        private readonly RenderUser _user2 = new("User2", Guid.Empty);
        
        public SelectSectionToApproveViewModelTests()
        {
            var user = new User("Consultant User", "consultant");
            _section = Section.UnitTestEmptySection();
            
            _step = new Step();
            _stage = new Stage();
            _stage.SetName("Stage");     
            var sectionsToApprove = new List<Section>() { _section };
            var mockSectionRepository = new Mock<ISectionRepository>();
            MockContextProvider.Setup(x => x.GetSectionRepository()).Returns(mockSectionRepository.Object);
            mockSectionRepository.Setup(x => x.GetSectionsForProjectAsync(It.IsAny<Guid>())).ReturnsAsync(sectionsToApprove);
            mockSectionRepository.Setup(x => x.GetSectionWithDraftsAsync(_section.Id, It.IsAny<bool>(), It
            .IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(_section);
            MockContextProvider.Setup(x => x.GetLoggedInUser());
            MockGrandCentralStation.Setup(x => x.SectionsAtStep(It.IsAny<Guid>()))
                .Returns(new List<Guid>{_section.Id});            

            var mockAudioPlayerService = new Mock<IAudioPlayerService>();
            mockAudioPlayerService.Setup(x => x.Duration).Returns(50);
            mockAudioPlayerService.Setup(x => x.AudioPlayerState).Returns(AudioPlayerState.Playing);
            MockContextProvider.Setup(x => x.GetAudioPlayerService(It.IsAny<Action>()))
                .Returns(mockAudioPlayerService.Object);

            var mockBarPlayerViewModel = new Mock<IBarPlayerViewModel>();
            mockBarPlayerViewModel.Setup(x => x.AudioPlayerState).Returns(AudioPlayerState.Playing);
            var mockMiniPlayerViewModel = new Mock<IMiniWaveformPlayerViewModel>();
            mockMiniPlayerViewModel.Setup(x => x.AudioPlayerState).Returns(AudioPlayerState.Playing);

            MockContextProvider.Setup(x => x.GetBarPlayerViewModel(
                It.IsAny<Audio>(), ActionState.Optional,It.IsAny<string>(), 0, null, null,
                It.IsAny<ImageSource>(), It.IsAny<ReactiveCommand<Unit,IRoutableViewModel>>(), 
                It.IsAny<IObservable<bool>>(), It.IsAny<string>())).Returns
                (mockBarPlayerViewModel
                .Object);
            MockContextProvider.Setup(x => x.GetMiniWaveformPlayerViewModel(
                It.IsAny<Audio>(), ActionState.Optional, It.IsAny<string>(), It.IsAny<TimeMarkers>(), null, It
                .IsAny<ImageSource>(), It.IsAny<ReactiveCommand<Unit,IRoutableViewModel>>(), It.IsAny<bool>(), It.IsAny<string>() )).Returns
                (mockMiniPlayerViewModel.Object);
            
            MockContextProvider.Setup(x => x.GetBarPlayerViewModel(
                It.IsAny<AudioPlayback>(), ActionState.Optional,It.IsAny<string>(), 0, null, null,
                It.IsAny<ImageSource>(), It.IsAny<ReactiveCommand<Unit,IRoutableViewModel>>(), 
                It.IsAny<IObservable<bool>>(), It.IsAny<string>())).Returns
            (mockBarPlayerViewModel
                .Object);
            MockContextProvider.Setup(x => x.GetMiniWaveformPlayerViewModel(
                It.IsAny<AudioPlayback>(), ActionState.Optional, It.IsAny<string>(), It.IsAny<TimeMarkers>(), null, It
                    .IsAny<ImageSource>(), It.IsAny<ReactiveCommand<Unit,IRoutableViewModel>>(), It.IsAny<bool>(), It.IsAny<string>() )).Returns
                (mockMiniPlayerViewModel.Object);
            MockContextProvider.Setup(x => x.GetLoggedInUser()).Returns(user);
            MockContextProvider.Setup(x => x.GetCurrentDeviceIdiom());
            MockContextProvider.Setup(x => x.GetSessionStateService()).Returns(new Mock<ISessionStateService>().Object);

            _renderWorkflow.AddTranslationAssignmentForTeam(_renderWorkflow.GetTeams().First(), _user1.Id);
            _renderWorkflow.AddTranslationAssignmentForTeam(_renderWorkflow.GetTeams().Last(), _user2.Id);
            _mockWorkflowRepository.Setup(x => x.GetWorkflowForProjectIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(_renderWorkflow);
            _mockSectionRepository.Setup(x => x.GetSectionsForProjectAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Section>{_section1, _section2});
            _mockUserRepository.Setup(x => x.GetUserAsync(_user1.Id)).ReturnsAsync(_user1);
            _mockUserRepository.Setup(x => x.GetUserAsync(_user2.Id)).ReturnsAsync(_user2);
            
            MockContextProvider.Setup(x => x.GetWorkflowRepository())
                .Returns(_mockWorkflowRepository.Object);
            MockContextProvider.Setup(x => x.GetUserRepository())
                .Returns(_mockUserRepository.Object);
            
            var mockSequencerFactory = new Mock<ISequencerFactory>();
            mockSequencerFactory.Setup(x => x.CreatePlayer(
                It.IsAny<Func<IAudioPlayer>>(),
                It.IsAny<FlagType>()
            )).Returns(new Mock<ISequencerPlayerViewModel>().Object);
            MockContextProvider.Setup(x => x.GetSequencerFactory()).Returns(mockSequencerFactory.Object);
            
            var mockTempAudioService = new Mock<ITempAudioService>();
            MockContextProvider.Setup(x => x.GetTempAudioService(It.IsAny<Audio>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(mockTempAudioService.Object);
            MockContextProvider.Setup(x => x.GetTempAudioService(It.IsAny<AudioPlayback>())).Returns(mockTempAudioService.Object);
            mockTempAudioService.Setup(s => s.SaveTempAudio()).Returns("temp.wav");
        }

        [Fact]
        public async Task ViewModelCreation_Project_Exist_Populate_List()
        {
            //Arrange
            var contentProvider = MockContextProvider.Object;

            //Act
            var vm = await SelectSectionToApproveViewModel.CreateAsync(contentProvider, _step, _stage);

            //Assert
            vm.SectionsToApprove.Items.Should().NotBeEmpty();
        }
        
        [Fact]
        public async Task Select_Section_NavigatesToSection()
        {
            //Arrange
            MockGrandCentralStation.Setup(x => x.GetStepToWorkAsync(It.IsAny<Guid>(), It.IsAny<RenderStepTypes>()))
            .ReturnsAsync(new Step());
            var contentProvider = MockContextProvider.Object;
            var vm = await SelectSectionToApproveViewModel.CreateAsync(contentProvider, _step, _stage);
            var sectionToApprove = vm.SectionsToApprove.Items.First();
            SetupViewModelForNavigationTest(vm);
            SetupViewModelForNavigationTest(sectionToApprove);
            
            //Act
            sectionToApprove.NavigateToSectionCommand.Execute().Subscribe();
            
            //Assert
            await VerifyNavigationResultAsync<ApproveSectionViewModel>(sectionToApprove.NavigateToSectionCommand);
        }
        
        [Fact]
        public async Task ApproveSection_Updates_SectionList()
        {
            //Arrange
            var contentProvider = MockContextProvider.Object;
            var vm = await SelectSectionToApproveViewModel.CreateAsync(contentProvider, _step, _stage);
            var sectionToApproveCard = vm.SectionsToApprove.Items.First();
            SetupViewModelForNavigationTest(vm);

            //Act
            sectionToApproveCard.RemoveApproveSectionAction.Invoke(_section);

            //Assert
            vm.SectionsToApprove.Items.Should().BeEmpty();
        }
    }
}