using FluentAssertions;
using Moq;
using Render.Components.StageSettings.CommunityTestStageSettings;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.WorkflowRepositories;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Components.StageSettings
{
    public class CommunityTestStageSettingsViewModelTests : ViewModelTestBase
    {
        private readonly RenderWorkflow _workflow = RenderWorkflow.Create(Guid.Empty);
        
        private readonly Stage _communityStage;

        public CommunityTestStageSettingsViewModelTests()
        {
            var workflowPersistence = new Mock<IWorkflowRepository>();
            _workflow.BuildDefaultWorkflow();
            _communityStage = _workflow.GetCustomStages().First(x => x.StageType == StageTypes.CommunityTest);

            MockContextProvider.Setup(x => x.GetWorkflowRepository())
                .Returns(workflowPersistence.Object);
            MockContextProvider.Setup(x => x.GetModalService())
                .Returns(new Mock<IModalService>().Object);
        }
        
        private CommunityTestStageSettingsViewModel GetViewModel()
        {
            return new CommunityTestStageSettingsViewModel(
                _workflow,
                _communityStage,
                MockContextProvider.Object,
                (_) => { });
        }

        [Fact]
        public void TurnOffDoRetell_TurnsOfAllSettings()
        {
            //Arrange
            var vm = GetViewModel();
            
            //Act
            vm.SelectedState = RetellQuestionResponseSettings.QuestionAndResponse;
            
            //Assert
            vm.RetellRequirePassageListen.Should().BeFalse();
        }

        [Fact]
        public void TurnOnDoRetell_TurnsOnAllSettings()
        {
            //Arrange
            var vm = GetViewModel();
            
            //Act
            vm.SelectedState = RetellQuestionResponseSettings.QuestionAndResponse;
            vm.SelectedState = RetellQuestionResponseSettings.Retell;
            
            //Assert
            vm.RetellRequirePassageListen.Should().BeTrue();
            vm.RetellRequireSectionListen.Should().BeTrue();
        }

        [Fact]
        public void TurnOffResponse_TurnsOffAllSettings()
        {
            //Arrange
            var vm = GetViewModel();
            
            //Act
            vm.SelectedState = RetellQuestionResponseSettings.Retell;
            
            //Assert
            vm.ResponseRequireContextListen.Should().BeFalse();
            vm.ResponseRequireRecordResponse.Should().BeFalse();
        }

        [Fact]
        public void TurnOnResponse_TurnsOnAllSettings()
        {
            //Arrange
            var vm = GetViewModel();
            
            //Act
            vm.SelectedState = RetellQuestionResponseSettings.Retell;
            vm.SelectedState = RetellQuestionResponseSettings.QuestionAndResponse;
            
            //Assert
            vm.ResponseRequireContextListen.Should().BeTrue();
            vm.ResponseRequireRecordResponse.Should().BeTrue();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void UpdateWorkflow_ReviewStep_SettingsUpdatedCorrectly(bool expected)
        {
            //Arrange
            var vm = GetViewModel();
            var reviewStep = _communityStage.Steps.First(x => x.RenderStepType == RenderStepTypes.CommunityTest);

            //Act
            vm.RetellRequirePassageListen = expected;
            vm.RetellRequireSectionListen = expected;
            vm.AssignToTranslator = expected;
            vm.ResponseRequireContextListen = expected;
            vm.ResponseRequireRecordResponse = expected;

            vm.ConfirmCommand.Execute().Subscribe();
            
            //Assert
            _communityStage.StageSettings.GetSetting(SettingType.AssignToTranslator).Should().Be(expected);
            
            reviewStep.StepSettings.GetSetting(SettingType.RequirePassageListen).Should().Be(expected);
            reviewStep.StepSettings.GetSetting(SettingType.RequireSectionListen).Should().Be(expected);
            reviewStep.StepSettings.GetSetting(SettingType.RequireQuestionContextListen).Should().Be(expected);
            reviewStep.StepSettings.GetSetting(SettingType.RequireRecordResponse).Should().Be(expected);
        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void UpdateWorkflow_ReviseStep_SettingsUpdatedCorrectly(bool expected)
        {
            //Arrange
            var vm = GetViewModel();
            var reviseStep  = _communityStage.Steps.First(x => x.RenderStepType == RenderStepTypes.CommunityRevise);

            //Act
            vm.ReviseAllowEditing = expected;
            vm.ReviseRequireSectionListen = expected;
            vm.ReviseRequireCommunityFeedback = expected;
            
            vm.ConfirmCommand.Execute().Subscribe();
            
            //Assert
            reviseStep.StepSettings.GetSetting(SettingType.AllowEditing).Should().Be(expected);
            reviseStep.StepSettings.GetSetting(SettingType.RequireSectionListen).Should().Be(expected);
            reviseStep.StepSettings.GetSetting(SettingType.RequireCommunityFeedback).Should().Be(expected);
        }
    }
}