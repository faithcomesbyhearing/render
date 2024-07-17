using FluentAssertions;
using Moq;
using Render.Components.StageSettings.DraftingStageSettings;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Workflow;
using Render.Repositories.WorkflowRepositories;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Components.StageSettings
{
    public class DraftingStageSettingsViewModelTests : ViewModelTestBase
    {
        private readonly RenderWorkflow _workflow = RenderWorkflow.Create(Guid.Empty);

        public DraftingStageSettingsViewModelTests()
        {
            var workflowPersistence = new Mock<IWorkflowRepository>();
            MockContextProvider.Setup(x => x.GetWorkflowRepository())
                .Returns(workflowPersistence.Object);
            MockContextProvider.Setup(x => x.GetModalService())
                .Returns(new Mock<IModalService>().Object);
        }
        
        private DraftingStageSettingsViewModel GetViewModel()
        {
            return new DraftingStageSettingsViewModel(
                _workflow,
                _workflow.DraftingStage,
                MockContextProvider.Object,
                (_) => { });
        }

        [Fact]
        public void TurnOffDoSectionListen_TurnsOffRequireSectionListen()
        {
            //Arrange
            var vm = GetViewModel();
            
            //Act
            vm.TranslateDoSectionListen = false;
            
            //Assert
            vm.TranslateRequireSectionListen.Should().BeFalse();
        }

        [Fact]
        public void TurnOnDoSectionListen_TurnsOnRequireSectionListen()
        {
            //Arrange
            var vm = GetViewModel();
            
            //Act
            vm.TranslateDoSectionListen = false;
            vm.TranslateDoSectionListen = true;
            
            //Assert
            vm.TranslateRequireSectionListen.Should().BeTrue();
        }

        [Fact]
        public void TurnOffDoPassageListen_TurnsOffRequirePassageListen()
        {
            //Arrange
            var vm = GetViewModel();

            //Act
            vm.TranslateDoPassageListen = false;
            
            //Assert
            vm.TranslateRequirePassageListen.Should().BeFalse();
        }
        [Fact]
        public void TurnOnDoPassageListen_TurnsOnRequirePassageListen()
        {
            //Arrange
            var vm = GetViewModel();

            //Act
            vm.TranslateDoPassageListen = false;
            vm.TranslateDoPassageListen = true;
            
            //Assert
            vm.TranslateRequirePassageListen.Should().BeTrue();
        }

        [Fact]
        public void TurnOffDoPassageReview_TurnsOffRequirePassageReview()
        {
            //Arrange
            var vm = GetViewModel();

            //Act
            vm.TranslateDoPassageReview = false;
            
            //Assert
            vm.TranslateRequirePassageReview.Should().BeFalse();
        }
        [Fact]
        public void TurnOnDoPassageReview_TurnsOnRequirePassageReview()
        {
            //Arrange
            var vm = GetViewModel();

            //Act
            vm.TranslateDoSectionReview = false;
            vm.TranslateDoSectionReview = true;
            
            //Assert
            vm.TranslateRequireSectionReview.Should().BeTrue();
        }


        [Fact]
        public void UpdateWorkflow_WithAllRequirementsOff_TurnsAllOff()
        {
            //Arrange
            var vm = GetViewModel();
            
            //Act
            vm.TranslateRequirePassageListen = false;
            vm.TranslateRequirePassageReview = false;
            vm.TranslateRequireSectionListen = false;
            vm.TranslateRequireSectionReview = false;
            vm.ConfirmCommand.Execute().Subscribe();
            
            //Assert
            
            var step = _workflow.DraftingStage.Steps.First();
            step.StepSettings.GetSetting(SettingType.DoPassageListen).Should().BeTrue();
            step.StepSettings.GetSetting(SettingType.RequirePassageListen).Should().BeFalse();
            step.StepSettings.GetSetting(SettingType.DoSectionListen).Should().BeTrue();
            step.StepSettings.GetSetting(SettingType.RequireSectionListen).Should().BeFalse();
            step.StepSettings.GetSetting(SettingType.DoPassageReview).Should().BeTrue();
            step.StepSettings.GetSetting(SettingType.RequirePassageReview).Should().BeFalse();
            step.StepSettings.GetSetting(SettingType.DoSectionReview).Should().BeTrue();
            step.StepSettings.GetSetting(SettingType.RequireSectionReview).Should().BeFalse();
        }
    }
}