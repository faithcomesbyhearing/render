using FluentAssertions;
using Moq;
using Render.Components.StageSettings.PeerCheckStageSettings;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.WorkflowRepositories;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Components.StageSettings
{
    public class PeerCheckStageSettingsViewModelTests : ViewModelTestBase
    {
        private readonly RenderWorkflow _workflow = RenderWorkflow.Create(Guid.Empty);
        private readonly Stage _peerStage;

        public PeerCheckStageSettingsViewModelTests()
        {
            var workflowPersistence = new Mock<IWorkflowRepository>();
            _workflow.BuildDefaultWorkflow();
            _peerStage = _workflow.GetCustomStages().First(x => x.StageType == StageTypes.PeerCheck);
            MockContextProvider.Setup(x => x.GetWorkflowRepository())
                .Returns(workflowPersistence.Object);
            MockContextProvider.Setup(x => x.GetModalService())
                .Returns(new Mock<IModalService>().Object);
        }
        
        private PeerCheckStageSettingsViewModel GetViewModel()
        {
            return new PeerCheckStageSettingsViewModel(
                _workflow,
                _peerStage,
                MockContextProvider.Object,
                (_) => { });
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
        public void TurnOffDoSectionReview_TurnsOffRequireSectionReview()
        {
            //Arrange
            var vm = GetViewModel();

            //Act
            vm.SelectedState = SelectedState.PassageListenOnly;
            
            //Assert
            vm.CheckRequireSectionListen.Should().BeFalse();
        }

        [Fact]
        public void TurnOnDoSectionReview_TurnsOnRequireSectionReview()
        {
            //Arrange
            var vm = GetViewModel();

            //Act
            vm.SelectedState = SelectedState.PassageListenOnly;
            vm.SelectedState = SelectedState.SectionListenOnly;
            
            //Assert
            vm.CheckRequireSectionListen.Should().BeTrue();
        }

        [Fact]
        public void TurnOffCheckDoPassageReview_TurnsOffRequirePassageReview()
        {
            //Arrange
            var vm = GetViewModel();

            //Act
            vm.SelectedState = SelectedState.SectionListenOnly;
            
            //Assert
            vm.CheckRequirePassageListen.Should().BeFalse();
        }

        [Fact]
        public void TurnOnCheckDoPassageReview_TurnsOnRequirePassageReview()
        {
            //Arrange
            var vm = GetViewModel();

            //Act
            vm.SelectedState = SelectedState.SectionListenOnly;
            vm.SelectedState = SelectedState.PassageListenOnly;
            
            //Assert
            vm.CheckRequirePassageListen.Should().BeTrue();
        }

        [Fact]
        public void UpdateWorkflow_WithAllRequirementsOff_TurnsAllOff()
        {
            //Arrange
            var vm = GetViewModel();
            
            //Act
            vm.NoSelfCheck = false;
            vm.CheckRequirePassageListen = false;
            vm.CheckRequireSectionListen = false;
            vm.TranslateRequirePassageReview = false;
            vm.TranslateRequireSectionListen = false;
            vm.TranslateRequireSectionReview = false;
            vm.ConfirmCommand.Execute().Subscribe();
            
            //Assert
            _peerStage.StageSettings.GetSetting(SettingType.NoSelfCheck).Should().BeFalse();

            var checkStep = _peerStage.Steps.First(x => x.RenderStepType == RenderStepTypes.PeerCheck);
            checkStep.StepSettings.GetSetting(SettingType.DoPassageReview).Should().BeTrue();
            checkStep.StepSettings.GetSetting(SettingType.RequirePassageReview).Should().BeFalse();
            checkStep.StepSettings.GetSetting(SettingType.DoSectionReview).Should().BeTrue();
            checkStep.StepSettings.GetSetting(SettingType.RequireSectionReview).Should().BeFalse();
            
            var reviseStep = _peerStage.Steps.First(x => x.RenderStepType == RenderStepTypes.PeerRevise);
            reviseStep.StepSettings.GetSetting(SettingType.DoPassageReview).Should().BeTrue();
            reviseStep.StepSettings.GetSetting(SettingType.RequirePassageReview).Should().BeFalse();
        }
    }
}