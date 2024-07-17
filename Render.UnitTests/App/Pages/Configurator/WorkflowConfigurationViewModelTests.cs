using FluentAssertions;
using Moq;
using Render.Models.Workflow;
using Render.Pages.Configurator.WorkflowManagement;
using Render.Repositories.Kernel;
using Render.Repositories.WorkflowRepositories;
using Render.TempFromVessel.Project;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Configurator
{
    public class WorkflowConfigurationViewModelTests : ViewModelTestBase
    {
        private readonly Guid _projectId;
        private readonly Mock<IWorkflowRepository> _mockWorkflowRepository;

        public WorkflowConfigurationViewModelTests()
        {
            _projectId = Guid.NewGuid();

            var renderWorkflow = RenderWorkflow.Create(Guid.NewGuid());
            renderWorkflow.BuildDefaultWorkflow();

            _mockWorkflowRepository = new Mock<IWorkflowRepository>();
            _mockWorkflowRepository.Setup(x => x.GetWorkflowForProjectIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(renderWorkflow);

            MockContextProvider.Setup(x => x.GetWorkflowRepository()).Returns(_mockWorkflowRepository.Object);

            var projectRepositoryMock = new Mock<IDataPersistence<Project>>();
            var project = new Project("Project Name", string.Empty, string.Empty);
            projectRepositoryMock.Setup(x => x.GetAsync(_projectId)).ReturnsAsync(project);
            MockContextProvider.Setup(x => x.GetPersistence<Project>()).Returns(projectRepositoryMock.Object);
        }

        [Fact]
        public async Task Create_WithExistingWorkflow_DoesNotCreateNewWorkflow()
        {
            //Arrange
            //Act
            await WorkflowConfigurationViewModel.CreateAsync(_projectId, MockContextProvider.Object);
            //Assert
            _mockWorkflowRepository.Verify(x => x.SaveWorkflowAsync(It.IsAny<RenderWorkflow>()), Times.Never);
        }

        [Fact]
        public async Task Create_WithNoWorkflow_HasProperStageCardsForBlankWorkflow()
        {
            //Arrange
            _mockWorkflowRepository.Setup(x => x.GetWorkflowForProjectIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(RenderWorkflow.Create(Guid.Empty));
            //Act
            var vm = await WorkflowConfigurationViewModel.CreateAsync(_projectId, MockContextProvider.Object);
            //Assert
            vm.StageCards.Count.Should().Be(2);
            vm.StageCards.First().ShowAddStepAfterCard.Should().BeTrue();
            vm.StageCards.First().Stage.StageType.Should().Be(StageTypes.Drafting);
            vm.StageCards.Last().Stage.StageType.Should().Be(StageTypes.ConsultantApproval);
        }

        [Fact]
        public async Task Create_WithDefaultWorkflow_HasProperCards()
        {
            //Arrange
            //Act
            var vm = await WorkflowConfigurationViewModel.CreateAsync(_projectId, MockContextProvider.Object);
            //Assert
            vm.StageCards.Count.Should().Be(5);
        }

        [Fact]
        public async Task AddPeerStage_AddsCorrectStageInCorrectPlace()
        {
            //Arrange
            //Act
            var vm = await WorkflowConfigurationViewModel.CreateAsync(_projectId, MockContextProvider.Object);
            var draftStage = vm.StageCards.First();
            draftStage.AddStageCommand.Execute(StageTypes.PeerCheck).Subscribe();

            //Assert
            _mockWorkflowRepository.Verify(x => x.SaveWorkflowAsync(It.IsAny<RenderWorkflow>()));
            vm.StageCards.Count.Should().Be(6);
            vm.StageCards.Skip(1).Take(1).First().Stage.StageType.Should().Be(StageTypes.PeerCheck);
        }

        [Fact]
        public async Task AddCommunityStage_AddsCorrectStageInCorrectPlace()
        {
            //Arrange
            //Act
            var vm = await WorkflowConfigurationViewModel.CreateAsync(_projectId, MockContextProvider.Object);
            var draftStage = vm.StageCards.First();
            draftStage.AddStageCommand.Execute(StageTypes.CommunityTest).Subscribe();

            //Assert
            _mockWorkflowRepository.Verify(x => x.SaveWorkflowAsync(It.IsAny<RenderWorkflow>()));
            vm.StageCards.Count.Should().Be(6);
            vm.StageCards.Skip(1).Take(1).First().Stage.StageType.Should().Be(StageTypes.CommunityTest);
        }

        [Fact]
        public async Task AddConsultantStage_AddsCorrectStageInCorrectPlace()
        {
            //Arrange
            //Act
            var vm = await WorkflowConfigurationViewModel.CreateAsync(_projectId, MockContextProvider.Object);
            var draftStage = vm.StageCards.First();
            draftStage.AddStageCommand.Execute(StageTypes.ConsultantCheck).Subscribe();

            //Assert
            _mockWorkflowRepository.Verify(x => x.SaveWorkflowAsync(It.IsAny<RenderWorkflow>()));
            vm.StageCards.Count.Should().Be(6);
            vm.StageCards.Skip(1).Take(1).First().Stage.StageType.Should().Be(StageTypes.ConsultantCheck);
        }

        [Fact]
        public async Task AddStageCommand_WhenConsultantCheckStage_GetWorkflowRepositoryIsCalled()
        {
            //Arrange

            //Act
            var vm = await WorkflowConfigurationViewModel.CreateAsync(_projectId, MockContextProvider.Object);
            var draftStage = vm.StageCards.First();
            draftStage.AddStageCommand.Execute(StageTypes.ConsultantCheck).Subscribe();

            //Assert
            MockContextProvider.Verify(x => x.GetWorkflowRepository(), Times.AtLeast(1));
            _mockWorkflowRepository.Verify(x => x.GetWorkflowForProjectIdAsync(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task DeleteStage_DeletesProperStage()
        {
            //Arrange
            //Act
            var vm = await WorkflowConfigurationViewModel.CreateAsync(_projectId, MockContextProvider.Object);
            var communityStage = vm.StageCards.First(x => x.Stage.StageType == StageTypes.CommunityTest);
            communityStage.DeleteStageCommand.Execute().Subscribe(Render.Kernel.Stubs.ActionNop, Render.Kernel.Stubs.ExceptionNop);

            //Assert
            vm.StageCards.Count.Should().Be(5);
            vm.StageCards.Any(x => x.Stage.StageType == StageTypes.CommunityTest).Should().Be(true);
        }

        [Fact]
        public async Task OpenStageSettingsCommand_NavigateToStageSettingsPageAsync()
        {
            //Arrange
            var vm = await WorkflowConfigurationViewModel.CreateAsync(_projectId, MockContextProvider.Object);
            var communityStage = vm.StageCards.First(x => x.Stage.StageType == StageTypes.CommunityTest);

            //Act
            communityStage.OpenStageSettingsCommand.Execute().Subscribe();

            //Assert
            vm.UrlPathSegment.Should().Be("WorkflowConfiguration");
        }
    }
}