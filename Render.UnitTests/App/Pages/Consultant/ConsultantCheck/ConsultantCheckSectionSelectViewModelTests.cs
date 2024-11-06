using System.Reactive;
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
using Render.Pages.Consultant.ConsultantCheck;
using Render.Repositories.SectionRepository;
using Render.Services.AudioServices;
using Render.Services.WorkflowService;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Consultant.ConsultantCheck
{
    public class ConsultantCheckSectionSelectViewModelTests : ViewModelTestBase
    {
        private readonly Section _section;
        private readonly Stage _stage;
        private Step _step;

        public ConsultantCheckSectionSelectViewModelTests()
        {
            var user = new User("Consultant User", "consultant");
            _stage = new Stage();
            _stage.SetName("Stage");
            _step = new Step(RenderStepTypes.ConsultantCheck);
            _section = Section.UnitTestEmptySection();
            var audio = new Audio(Guid.Empty, _section.ProjectId, _section.Id);
            var mockAudioPlayerService = new Mock<IAudioPlayerService>();
            var mockSectionRepository = new Mock<ISectionRepository>();
            mockSectionRepository.Setup(x => x.GetSectionsForProjectAsync(_section.Id))
                .ReturnsAsync(new List<Section>() { _section });
            MockContextProvider.Setup(x => x.GetSectionRepository()).Returns(mockSectionRepository.Object);
            mockSectionRepository.Setup(x => x.GetSectionAsync(It.IsAny<Guid>(), false))
                .ReturnsAsync(_section);
            mockAudioPlayerService.Setup(x => x.Duration).Returns(50);
            mockAudioPlayerService.Setup(x => x.AudioPlayerState).Returns(AudioPlayerState.Playing);

            var mockBarPlayerViewModel = new Mock<IBarPlayerViewModel>();
            mockBarPlayerViewModel.Setup(x => x.AudioPlayerState).Returns(AudioPlayerState.Playing);
            var mockMiniPlayerViewModel = new Mock<IMiniWaveformPlayerViewModel>();
            mockMiniPlayerViewModel.Setup(x => x.AudioPlayerState).Returns(AudioPlayerState.Playing);
            
            MockContextProvider.Setup(x => x.GetBarPlayerViewModel(
                It.IsAny<Audio>(), ActionState.Optional,It.IsAny<string>(), 1, null, null, 
                It.IsAny<ImageSource>(), It.IsAny<ReactiveCommand<Unit,IRoutableViewModel>>(), 
                It.IsAny<IObservable<bool>>(), It.IsAny<string>())).Returns(mockBarPlayerViewModel.Object);
            MockContextProvider.Setup(x => x.GetMiniWaveformPlayerViewModel(
                It.IsAny<Audio>(), ActionState.Optional, It.IsAny<string>(), It.IsAny<TimeMarkers>(), 
                null, null, null,It.IsAny<bool>(), It.IsAny<string>())).Returns(mockMiniPlayerViewModel
                .Object);
            MockContextProvider.Setup(x => x.GetLoggedInUser()).Returns(user);
            MockContextProvider.Setup(x => x.GetCurrentDeviceIdiom());

            var workflow = RenderWorkflow.Create(Guid.Empty);
            workflow.AddStage(ConsultantCheckStage.Create());
            var mockWorkflowService = new Mock<IWorkflowService>();
            mockWorkflowService.Setup(x => x.ProjectWorkflow).Returns(workflow);

            MockStageService.Setup(x => 
                    x.SectionsAtStep(It.IsAny<Guid>())).Returns(new List<Guid> { _section.Id });
        }
        
        [Fact]
        public async Task ViewModelCreation_Project_Exist_Populate_List()
        {
            //Arrange
            var contentProvider = MockContextProvider.Object;
            var projectId = _section.Id;

            //Act
            var vm = await ConsultantCheckSectionSelectViewModel.CreateAsync(projectId, contentProvider, _stage, _step);

            //Assert
            vm.SectionsToCheck.Items.Should().NotBeEmpty();
        }
        
        [Fact]
        public async Task SetChecked_MovesToCorrectList()
        {
            //Arrange
            var contentProvider = MockContextProvider.Object;
            var projectId = _section.Id;

            //Act
            var vm = await ConsultantCheckSectionSelectViewModel.CreateAsync(projectId, contentProvider, _stage, _step);

            //Assert
            vm.SectionsToCheck.Items.Should().NotBeEmpty();
            MockStageService.Verify(x => x.SectionsAtStep(It.IsAny<Guid>()), Times.AtLeastOnce);
            MockContextProvider.Verify(x => x.GetGrandCentralStation(), Times.AtLeastOnce);
            MockContextProvider.Verify(x => x.GetGrandCentralStation(), Times.AtLeastOnce);
        }
        
        [Fact]
        public async Task CheckedSectionsProperty_WhenHaveAssignedSection_Success()
        {
            //Arrange
            var contentProvider = MockContextProvider.Object;
            var projectId = _section.Id;

            _step = new Step(RenderStepTypes.ConsultantRevise, Roles.Drafting);
            _stage.AddRevisePreparationStep(_step);
            var reviseStep = _stage.Steps.FirstOrDefault();
            MockStageService.Setup(x => 
                x.GetAllAssignedSectionAtStep(reviseStep.Id, reviseStep.RenderStepType)).Returns(new List<Guid> { _section.Id });
            //Act
            var vm = await ConsultantCheckSectionSelectViewModel.CreateAsync(projectId, contentProvider, _stage, _step);

            //Assert
            vm.CheckedSections.Items.Should().NotBeEmpty();
            MockStageService.Verify(x => x.GetAllAssignedSectionAtStep(reviseStep.Id, reviseStep.RenderStepType), Times.AtLeastOnce);
            MockContextProvider.Verify(x => x.GetGrandCentralStation(), Times.AtLeastOnce);
        }
        
        [Fact]
        public async Task CheckedSectionsProperty_WhenNoAssignedSectionsAtStep_Success()
        {
            //Arrange
            var contentProvider = MockContextProvider.Object;
            var projectId = _section.Id;

            _step = new Step(RenderStepTypes.ConsultantRevise, Roles.Drafting);
            _stage.AddRevisePreparationStep(_step);
            var reviseStep = _stage.Steps.FirstOrDefault();
            MockStageService.Setup(x => 
                x.GetAllAssignedSectionAtStep(reviseStep.Id, reviseStep.RenderStepType)).Returns(new List<Guid>());
            //Act
            var vm = await ConsultantCheckSectionSelectViewModel.CreateAsync(projectId, contentProvider, _stage, _step);

            //Assert
            vm.CheckedSections.Items.Should().BeEmpty();
            MockStageService.Verify(x => x.GetAllAssignedSectionAtStep(reviseStep.Id, reviseStep.RenderStepType), Times.AtLeastOnce);
            MockContextProvider.Verify(x => x.GetGrandCentralStation(), Times.AtLeastOnce);
        }
    }
}