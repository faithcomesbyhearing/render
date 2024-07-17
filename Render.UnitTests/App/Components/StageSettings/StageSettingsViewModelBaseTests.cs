using Moq;
using Render.Components.StageSettings.DraftingStageSettings;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Workflow;
using Render.Repositories.WorkflowRepositories;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Components.StageSettings
{
    public class StageSettingsViewModelBaseTests : ViewModelTestBase
    {
        private readonly RenderWorkflow _workflow = RenderWorkflow.Create(Guid.Empty);
        private readonly Mock<IWorkflowRepository> _workflowPersistence;

        public StageSettingsViewModelBaseTests()
        {
            _workflowPersistence = new Mock<IWorkflowRepository>();
            MockContextProvider.Setup(x => x.GetWorkflowRepository())
                .Returns(_workflowPersistence.Object);
            var mockModalService = new Mock<IModalService>();
            MockContextProvider.Setup(x => x.GetModalService())
                .Returns(mockModalService.Object);
        }
        
        [Fact]
        public void ConfirmCommand_CallsPersistenceAndClosesModal()
        {
            //Arrange
            var vm = new DraftingStageSettingsViewModel(
                _workflow,
                _workflow.DraftingStage,
                MockContextProvider.Object,
                (_) => { });
            
            //Act
            vm.ConfirmCommand.Execute().Subscribe();
            
            //Assert
            _workflowPersistence.Verify(x => x.SaveWorkflowAsync(_workflow));
        }
    }
}