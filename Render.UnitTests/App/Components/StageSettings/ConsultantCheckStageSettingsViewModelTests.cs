using FluentAssertions;
using Moq;
using Render.Components.StageSettings.ConsultantCheckStageSettings;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.WorkflowRepositories;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Components.StageSettings
{
    public class ConsultantCheckStageSettingsViewModelTests : ViewModelTestBase
    {
        private readonly RenderWorkflow _workflow = RenderWorkflow.Create(Guid.Empty);
        private readonly Stage _consultantStage;
        private readonly Step _backTranslateStep;
        private readonly Step _backTranslate2Step;
        private readonly Step _transcribeStep;
        private readonly Step _btMultiStep;
        private readonly List<Step> _noteInterpretSteps;

        public ConsultantCheckStageSettingsViewModelTests()
        {
            var workflowPersistence = new Mock<IWorkflowRepository>();
            _workflow.BuildDefaultWorkflow();
            _consultantStage = _workflow.GetCustomStages().First(x => x.StageType == StageTypes.ConsultantCheck);
            _btMultiStep = _consultantStage.Steps.First(x => x.Order == Step.Ordering.Parallel).GetSubSteps()
                .First(x => x.Role == Roles.BackTranslate);
            _backTranslateStep = _btMultiStep.GetSubSteps()
                .First(x => x.RenderStepType == RenderStepTypes.BackTranslate);
            _backTranslate2Step = _btMultiStep.GetSubSteps().First(x => x.Order == Step.Ordering.Parallel)
                .GetSubSteps().First(x => x.GetSubSteps().Count > 0).GetSubSteps()
                .First(x => x.Role == Roles.BackTranslate2);
            _transcribeStep = _btMultiStep.GetSubSteps().First(x => x.Order == Step.Ordering.Parallel)
                .GetSubSteps().First(x => x.RenderStepType == RenderStepTypes.Transcribe);

            _noteInterpretSteps = _consultantStage.Steps.Where(x => 
                    x.RenderStepType == RenderStepTypes.InterpretToConsultant ||
                    x.RenderStepType == RenderStepTypes.InterpretToTranslator)
                .ToList();
            MockContextProvider.Setup(x => x.GetWorkflowRepository())
                .Returns(workflowPersistence.Object);
            MockContextProvider.Setup(x => x.GetModalService())
                .Returns(new Mock<IModalService>().Object);
        }
        
        private ConsultantCheckStageSettingsViewModel GetViewModel()
        {
            return new ConsultantCheckStageSettingsViewModel(
                _workflow,
                _consultantStage,
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
        public void TurnOnDoPassageReview_TurnsOnRequirePassageReview()
        {
            //Arrange
            var vm = GetViewModel();

            //Act
            vm.TranslateDoPassageReview = false;
            vm.TranslateDoPassageReview = true;

            //Assert
            vm.TranslateDoPassageReview.Should().BeTrue();
        }

        [Fact]
        public void TurnOffRetellDoPassageReview_TurnsOffRetellRequirePassageReview()
        {
            //Arrange
            var vm = GetViewModel();
            
            //Act
            vm.RetellDoPassageReview = false;
            
            //Assert
            vm.RetellRequirePassageReview.Should().BeFalse();
        }

        [Fact]
        public void TurnOnRetellDoPassageReview_TurnsOnRetellRequirePassageReview()
        {
            //Arrange
            var vm = GetViewModel();
            
            //Act
            vm.RetellDoPassageReview = false;
            vm.RetellDoPassageReview = true;
            
            //Assert
            vm.RetellRequirePassageReview.Should().BeTrue();
        }

        [Fact]
        public void TurnOffSegmentDoPassageReview_TurnsOffSegmentRequirePassageReview()
        {
            //Arrange
            var vm = GetViewModel();
            
            //Act
            vm.SegmentDoPassageReview = false;
            
            //Assert
            vm.SegmentRequirePassageReview.Should().BeFalse();
        }

        [Fact]
        public void TurnOnSegmentDoPassageReview_TurnsOnSegmentRequirePassageReview()
        {
            //Arrange
            var vm = GetViewModel();
            
            //Act
            vm.SegmentDoPassageReview = false;
            vm.SegmentDoPassageReview = true;
            
            //Assert
            vm.SegmentRequirePassageReview.Should().BeTrue();
        }

        [Fact]
        public void TurnOffRetell_TurnsOffAllRetellSettings()
        {
            //Arrange
            var vm = GetViewModel();

            //Act
            vm.RetellIsActive = false;
            
            //Assert
            vm.RetellDoPassageReview.Should().BeFalse();
            vm.RetellRequirePassageReview.Should().BeFalse();
            vm.RetellRequireSectionListen.Should().BeFalse();
            vm.DoPassageTranscribeIsActive.Should().BeFalse();
        }

        [Fact]
        public void TurnOffSegment_TurnsOffAllSegmentSettings()
        {
            //Arrange
            var vm = GetViewModel();

            //Act
            vm.SegmentIsActive = false;
            
            //Assert
            vm.SegmentDoPassageReview.Should().BeFalse();
            vm.SegmentRequirePassageReview.Should().BeFalse();
            vm.SegmentRequirePassageListen.Should().BeFalse();
            vm.SegmentTranscribeIsActive.Should().BeFalse();
        }

        [Fact]
        public void TurnOffNoteInterpret_TurnsOffBothNoteInterpretSettings()
        {
            //Arrange
            var vm = GetViewModel();

            //Act
            vm.NoteInterpretIsActive = false;
            
            //Assert
            vm.RequireNoteListen.Should().BeFalse();
            vm.DoNoteReview.Should().BeFalse();
            vm.RequireNoteReview.Should().BeFalse();
        }
        
        [Fact]
        public void UpdateWorkflow_WithAllRequirementsOff_TurnsAllOff()
        {
            //Arrange
            _backTranslateStep.StepSettings.SetSetting(SettingType.DoSegmentBackTranslate, false);
            _backTranslateStep.StepSettings.SetSetting(SettingType.DoRetellBackTranslate, false);
            _transcribeStep.StepSettings.SetSetting(SettingType.DoSegmentTranscribe, false);
            _transcribeStep.StepSettings.SetSetting(SettingType.DoPassageTranscribe, false);
            var vm = GetViewModel();

            //Act
            vm.RetellIsActive = false;
            vm.SegmentIsActive = false;
            vm.NoteInterpretIsActive = false;
            vm.TranslateRequirePassageReview = false;
            vm.ConfirmCommand.Execute().Subscribe();

            //Assert
            _backTranslateStep.StepSettings.GetSetting(SettingType.DoRetellBackTranslate).Should().BeFalse();
            _backTranslateStep.StepSettings.GetSetting(SettingType.DoSegmentBackTranslate).Should().BeFalse();
            _transcribeStep.StepSettings.GetSetting(SettingType.DoSegmentTranscribe).Should().BeFalse();
            _transcribeStep.StepSettings.GetSetting(SettingType.DoPassageTranscribe).Should().BeFalse();

            var step = _consultantStage.Steps.First(x => x.RenderStepType == RenderStepTypes.ConsultantRevise);
            step.StepSettings.GetSetting(SettingType.DoPassageReview).Should().BeTrue();
            step.StepSettings.GetSetting(SettingType.RequirePassageReview).Should().BeFalse();
        }

        [Fact]
        public void UpdateWorkflow_WithStepRequirementsOff_UpdatesProperly()
        {
            //Arrange
            var vm = GetViewModel();

            //Act
            vm.RetellRequirePassageReview = false;
            vm.SegmentRequirePassageReview = false;
            vm.RequireNoteReview = false;
            vm.ConfirmCommand.Execute().Subscribe();
            
            //Assert
            _backTranslateStep.StepSettings.GetSetting(SettingType.RequireRetellBTPassageReview).Should().BeFalse();
            _backTranslateStep.StepSettings.GetSetting(SettingType.RequireSegmentBTPassageReview).Should().BeFalse();
            foreach (var step in _noteInterpretSteps)
            {
                step.StepSettings.GetSetting(SettingType.RequireNoteReview).Should().BeFalse();
            }
        }

        [Fact]
        public void TurnOn2StepBackTranslation_SetsAllProperValues()
        {
            //Arrange
            var vm = GetViewModel();
            
            //Act
            vm.RetellIsActive = true;
            vm.SegmentIsActive = true;
            vm.Do2StepBackTranslation = true;
            
            //Assert
            vm.Retell2IsActive.Should().BeTrue();
            vm.Segment2IsActive.Should().BeTrue();
        }

        [Fact]
        public void TurnOff2StepBackTranslation_SetsAllProperValues()
        {
            //Arrange
            var vm = GetViewModel();
            
            //Act
            vm.Do2StepBackTranslation = true;
            vm.Do2StepBackTranslation = false;

            //Assert
            vm.Retell2IsActive.Should().BeFalse();
            vm.Segment2IsActive.Should().BeFalse();
        }
        
        [Fact]
        public void TurnOnSettingFromRetell_Sets_RetellStepBackTranslation()
        {
            //Arrange
            var vm = GetViewModel();
            vm.DoStepBackTranslation = true;
        
            //Act
            vm.Retell2IsActive = true;
        
            //Assert
            vm.RetellStepBackTranslation.Should().BeTrue();
        }
        
        [Fact]
        public void TurnOnSettingFromSegment_Sets_SegmentStepBackTranslation()
        {
            //Arrange
            var vm = GetViewModel();
            vm.DoStepBackTranslation = true;
        
            //Act
            vm.Segment2IsActive = true;
        
            //Assert
            vm.SegmentIsActive.Should().BeTrue();
            vm.SegmentStepBackTranslation.Should().BeTrue();
        }
        
        [Fact]
        public void TurnOnDoStepBackTranslation_2StepBackTranslateOn_SetsSegmentAllProperValues()
        {
            //Arrange
            var vm = GetViewModel();
            vm.Do2StepBackTranslation = true;

            //Act
            vm.DoStepBackTranslation = true;

            //Assert
            vm.SegmentIsActive.Should().BeTrue();
            vm.SegmentTranscribeIsActive.Should().BeFalse();
            vm.Segment2IsActive.Should().BeTrue();
            vm.Segment2TranscribeIsActive.Should().BeFalse();
        }
        
        [Fact]
        public void TurnOnDoStepBackTranslation_2StepBackTranslateOff_SetsSegmentAllProperValues()
        {
            //Arrange
            var vm = GetViewModel();
            vm.Do2StepBackTranslation = false;

            //Act
            vm.DoStepBackTranslation = true;

            //Assert
            vm.SegmentIsActive.Should().BeTrue();
            vm.SegmentTranscribeIsActive.Should().BeFalse();
            vm.Segment2IsActive.Should().BeFalse();
            vm.Segment2TranscribeIsActive.Should().BeFalse();
        }
        
        [Fact]
        public void TurnOnDoStepBackTranslation_2StepBackTranslateOn_SetsRetellAllProperValues()
        {
            //Arrange
            var vm = GetViewModel();
            vm.Do2StepBackTranslation = true;

            //Act
            vm.DoStepBackTranslation = true;

            //Assert
            vm.RetellIsActive.Should().BeTrue();
            vm.DoPassageTranscribeIsActive.Should().BeFalse();
            vm.Retell2IsActive.Should().BeTrue();
            vm.DoPassage2TranscribeIsActive.Should().BeFalse();
        }
        
        [Fact]
        public void TurnOnDoStepBackTranslation_2StepBackTranslateOff_SetsRetellAllProperValues()
        {
            //Arrange
            var vm = GetViewModel();
            vm.Do2StepBackTranslation = false;

            //Act
            vm.DoStepBackTranslation = true;

            //Assert
            vm.RetellIsActive.Should().BeTrue();
            vm.DoPassageTranscribeIsActive.Should().BeFalse();
            vm.Retell2IsActive.Should().BeFalse();
            vm.DoPassage2TranscribeIsActive.Should().BeFalse();
        }
        
        [Fact]
        public void TurnOnRetellStepBackTranslation_SetsAllProperValues()
        {
            //Arrange
            var vm = GetViewModel();
            vm.DoStepBackTranslation = true;
            vm.Do2StepBackTranslation = true;

            //Act
            vm.RetellStepBackTranslation = true;

            //Assert
            vm.RetellIsActive.Should().BeTrue();
            vm.DoPassageTranscribeIsActive.Should().BeFalse();
            vm.Retell2IsActive.Should().BeTrue();
            vm.DoPassage2TranscribeIsActive.Should().BeFalse();
        }
        
        [Fact]
        public void TurnOffRetellStepBackTranslation_SetsAllProperValues()
        {
            //Arrange
            var vm = GetViewModel();
            vm.DoStepBackTranslation = true;
            vm.Do2StepBackTranslation = true;

            //Act
            vm.RetellStepBackTranslation = false;

            //Assert
            vm.RetellIsActive.Should().BeFalse();
            vm.DoPassageTranscribeIsActive.Should().BeFalse();
            vm.Retell2IsActive.Should().BeFalse();
            vm.DoPassage2TranscribeIsActive.Should().BeFalse();
        }
        
        [Fact]
        public void TurnOnSegmentStepBackTranslation_SetsAllProperValues()
        {
            //Arrange
            var vm = GetViewModel();
            vm.DoStepBackTranslation = true;
            vm.Do2StepBackTranslation = true;
            
            //Act
            vm.SegmentStepBackTranslation = true;

            //Assert
            vm.SegmentIsActive.Should().BeTrue();
            vm.SegmentTranscribeIsActive.Should().BeFalse();
            vm.Segment2IsActive.Should().BeTrue();
            vm.Segment2TranscribeIsActive.Should().BeFalse();
        }
        
        [Fact]
        public void TurnOffSegmentStepBackTranslation_SetsAllProperValues()
        {
            //Arrange
            var vm = GetViewModel();
            vm.DoStepBackTranslation = true;
            vm.Do2StepBackTranslation = true;

            //Act
            vm.SegmentStepBackTranslation = false;

            //Assert
            vm.SegmentIsActive.Should().BeFalse();
            vm.SegmentTranscribeIsActive.Should().BeFalse();
            vm.Segment2IsActive.Should().BeFalse();
            vm.Segment2TranscribeIsActive.Should().BeFalse();
        }
        
        [Fact]
        public void TurnOnStepBackTranslation_With2StepOff_WithSegmentBTOff_SetsAllProperValues()
        {
            //Arrange
            var vm = GetViewModel();
            vm.Do2StepBackTranslation = false;
            vm.SegmentStepBackTranslation = false;

            //Act
            vm.DoStepBackTranslation = false;
            vm.DoStepBackTranslation = true;

            //Assert
            vm.Segment2IsActive.Should().BeFalse();
            vm.Segment2TranscribeIsActive.Should().BeFalse();
        }
        
        [Fact]
        public void TurnOnStepBackTranslation_With2StepOff_WithRetellBTOff_SetsAllProperValues()
        {
            //Arrange
            var vm = GetViewModel();
            vm.Do2StepBackTranslation = false;
            vm.RetellStepBackTranslation = false;

            //Act
            vm.DoStepBackTranslation = false;
            vm.DoStepBackTranslation = true;

            //Assert
            vm.Retell2IsActive.Should().BeFalse();
            vm.DoPassage2TranscribeIsActive.Should().BeFalse();
        }
        
        [Fact]
        public void TurnOnDoStepBackTranslation_SetsAllProperValues()
        {
            //Arrange
            var vm = GetViewModel();

            //Act
            vm.DoStepBackTranslation = false;
            vm.DoStepBackTranslation = true;

            //Assert
            vm.RetellStepBackTranslation.Should().BeTrue();
            vm.SegmentStepBackTranslation.Should().BeTrue();
        }
        
        [Fact]
        public void TurnOffDoStepBackTranslation_SetsAllProperValues()
        {
            //Arrange
            var vm = GetViewModel();

            //Act
            vm.DoStepBackTranslation = true;
            vm.DoStepBackTranslation = false;

            //Assert
            vm.RetellStepBackTranslation.Should().BeFalse();
            vm.SegmentStepBackTranslation.Should().BeFalse();
        }
        
        [Fact]
        public void WorkflowWith2StepOn_HasOptionToViewSecondStep()
        {
            //Arrange
            var backTranslate2Step = _btMultiStep.GetSubSteps().First(x => x.Order == Step.Ordering.Parallel)
                .GetSubSteps().First(x => x.GetSubSteps().Count > 0).GetSubSteps()
                .First(x => x.Role == Roles.BackTranslate2);
            backTranslate2Step.StepSettings.SetSetting(SettingType.DoRetellBackTranslate, true);
            var vm = GetViewModel();
            vm.DoStepBackTranslation = false;
            vm.DoStepBackTranslation = true;
            
            //Assert
            vm.Do2StepBackTranslation.Should().BeTrue();
        }

        [Fact]
        public void TurnOn2Step_TurnOnTranscribe_Succeeds()
        {
            //Arrange
            _transcribeStep.StepSettings.SetSetting(SettingType.DoPassageTranscribe, false);
            _transcribeStep.StepSettings.SetSetting(SettingType.DoSegmentTranscribe, false);
            var vm = GetViewModel();
            vm.Do2StepBackTranslation = false;
            
            //Act1
            vm.DoPassageTranscribeIsActive = true;
            vm.SegmentTranscribeIsActive = true;
            vm.Do2StepBackTranslation = true;
            vm.DoPassage2TranscribeIsActive = true;
            vm.Segment2TranscribeIsActive = true;
            
            
            //Assert1
            vm.DoPassage2TranscribeIsActive.Should().BeTrue();
            vm.Segment2TranscribeIsActive.Should().BeTrue();
            //Act2
            vm.ConfirmCommand.Execute().Subscribe();
            
            //Assert2
            var transcribe2Step = _btMultiStep.GetSubSteps().First(x => x.Order == Step.Ordering.Parallel)
                .GetSubSteps().First(x => x.GetSubSteps().Count > 0).GetSubSteps()
                .First(x => x.RenderStepType == RenderStepTypes.Transcribe);
            transcribe2Step.StepSettings.GetSetting(SettingType.DoPassageTranscribe).Should().BeTrue();
            transcribe2Step.StepSettings.GetSetting(SettingType.DoSegmentTranscribe).Should().BeTrue();
        }

        [Fact]
        public void TurnOffRetell1_TurnsOffAllOfRetell1And2_AndDoesNotTouchSegment()
        {
            //Arrange
            _backTranslateStep.StepSettings.SetSetting(SettingType.DoSegmentBackTranslate, false);
            _backTranslateStep.StepSettings.SetSetting(SettingType.DoRetellBackTranslate, false);
            _backTranslateStep.StepSettings.SetSetting(SettingType.DoRetellBTPassageReview, false);
            _backTranslateStep.StepSettings.SetSetting(SettingType.RequireRetellBTPassageReview, false);
            _backTranslateStep.StepSettings.SetSetting(SettingType.RequireRetellBTPassageListen, false);
            _backTranslateStep.StepSettings.SetSetting(SettingType.RequireRetellBTSectionListen, false);
            _backTranslate2Step.StepSettings.SetSetting(SettingType.DoRetellBTPassageReview, false);
            _backTranslate2Step.StepSettings.SetSetting(SettingType.RequireRetellBTPassageReview, false);
            _backTranslate2Step.StepSettings.SetSetting(SettingType.RequireRetellBTPassageListen, false);
            _backTranslate2Step.StepSettings.SetSetting(SettingType.RequireRetellBTSectionListen, false);
            _transcribeStep.StepSettings.SetSetting(SettingType.DoPassageTranscribe, false);
            var vm = GetViewModel();
            vm.Do2StepBackTranslation = false;
            
            //Act
            vm.SegmentIsActive = true;
            vm.RetellIsActive = false;
            vm.Do2StepBackTranslation = true;
            
            //Assert
            vm.Retell2IsActive.Should().BeFalse();
            vm.DoPassageTranscribeIsActive.Should().BeFalse();
            vm.RetellDoPassageReview.Should().BeFalse();
            vm.RetellRequirePassageReview.Should().BeFalse();
            vm.RetellRequirePassageListen.Should().BeFalse();
            vm.RetellRequireSectionListen.Should().BeFalse();
            vm.DoPassage2TranscribeIsActive.Should().BeFalse();
            vm.Retell2DoPassageReview.Should().BeFalse();
            vm.Retell2RequirePassageListen.Should().BeFalse();
            vm.Retell2RequirePassageReview.Should().BeFalse();
            vm.Retell2RequireSectionListen.Should().BeFalse();
            vm.AllowTurnOnRetell2.Should().BeFalse();
            
            vm.SegmentIsActive.Should().BeTrue();
            vm.SegmentDoPassageReview.Should().BeTrue();
            vm.SegmentRequirePassageReview.Should().BeTrue();
            vm.SegmentRequirePassageListen.Should().BeTrue();
            vm.SegmentRequireSectionListen.Should().BeTrue();
            vm.Segment2IsActive.Should().BeTrue();
            vm.Segment2DoPassageReview.Should().BeTrue();
            vm.Segment2RequirePassageListen.Should().BeTrue();
            vm.Segment2RequirePassageReview.Should().BeTrue();
            vm.Segment2RequireSectionListen.Should().BeTrue();
            vm.AllowTurnOnSegment2.Should().BeTrue();
        }

        [Fact]
        public void TurnOffSegment1_TurnsOffAllOfSegment1And2_AndDoesNotTouchRetell()
        {
            //Arrange
            _backTranslateStep.StepSettings.SetSetting(SettingType.DoSegmentBackTranslate, false);
            _backTranslateStep.StepSettings.SetSetting(SettingType.DoRetellBackTranslate, false);
            _backTranslateStep.StepSettings.SetSetting(SettingType.DoSegmentBTPassageReview, false);
            _backTranslateStep.StepSettings.SetSetting(SettingType.RequireSegmentBTPassageReview, false);
            _backTranslateStep.StepSettings.SetSetting(SettingType.RequireSegmentBTPassageListen, false);
            _backTranslateStep.StepSettings.SetSetting(SettingType.RequireSegmentBTSectionListen, false);
            _backTranslate2Step.StepSettings.SetSetting(SettingType.DoSegmentBTPassageReview, false);
            _backTranslate2Step.StepSettings.SetSetting(SettingType.RequireSegmentBTPassageReview, false);
            _backTranslate2Step.StepSettings.SetSetting(SettingType.RequireSegmentBTPassageListen, false);
            _backTranslate2Step.StepSettings.SetSetting(SettingType.RequireSegmentBTSectionListen, false);
            _transcribeStep.StepSettings.SetSetting(SettingType.DoPassageTranscribe, false);
            _transcribeStep.StepSettings.SetSetting(SettingType.DoSegmentTranscribe, false);
            var vm = GetViewModel();
            vm.Do2StepBackTranslation = false;
            
            //Act
            vm.RetellIsActive = true;
            vm.SegmentIsActive = false;
            vm.DoPassageTranscribeIsActive = true;
            vm.Do2StepBackTranslation = true;
            vm.DoPassage2TranscribeIsActive = true;

            //Assert
            vm.Segment2IsActive.Should().BeFalse();
            vm.SegmentTranscribeIsActive.Should().BeFalse();
            vm.SegmentDoPassageReview.Should().BeFalse();
            vm.SegmentRequirePassageReview.Should().BeFalse();
            vm.SegmentRequirePassageListen.Should().BeFalse();
            vm.SegmentRequireSectionListen.Should().BeFalse();
            vm.Segment2TranscribeIsActive.Should().BeFalse();
            vm.Segment2DoPassageReview.Should().BeFalse();
            vm.Segment2RequirePassageListen.Should().BeFalse();
            vm.Segment2RequirePassageReview.Should().BeFalse();
            vm.Segment2RequireSectionListen.Should().BeFalse();
            vm.AllowTurnOnSegment2.Should().BeFalse();

            vm.RetellIsActive.Should().BeTrue();
            vm.DoPassageTranscribeIsActive.Should().BeTrue();
            vm.RetellDoPassageReview.Should().BeTrue();
            vm.RetellRequirePassageReview.Should().BeTrue();
            vm.RetellRequirePassageListen.Should().BeTrue();
            vm.RetellRequireSectionListen.Should().BeTrue();
            vm.Retell2IsActive.Should().BeTrue();
            vm.DoPassage2TranscribeIsActive.Should().BeTrue();
            vm.Retell2DoPassageReview.Should().BeTrue();
            vm.Retell2RequirePassageListen.Should().BeTrue();
            vm.Retell2RequirePassageReview.Should().BeTrue();
            vm.Retell2RequireSectionListen.Should().BeTrue();
            vm.AllowTurnOnRetell2.Should().BeTrue();
            vm.AllowStepBackTranslation.Should().BeTrue();
        }

        [Fact]
        public void TurnOffSegment2_TurnsOffAllOfSegment2_AndNoneOfSegment1()
        {
            //Arrange
            _backTranslateStep.StepSettings.SetSetting(SettingType.DoSegmentBackTranslate, false);
            _transcribeStep.StepSettings.SetSetting(SettingType.DoSegmentTranscribe, false);
            var vm = GetViewModel();
            vm.Do2StepBackTranslation = false;
            
            //Act
            vm.SegmentIsActive = true;
            vm.SegmentTranscribeIsActive = true;
            vm.Do2StepBackTranslation = true;
            vm.Segment2IsActive = false;

            //Assert
            vm.SegmentIsActive.Should().BeTrue();
            vm.SegmentTranscribeIsActive.Should().BeTrue();
            vm.SegmentDoPassageReview.Should().BeTrue();
            vm.SegmentRequirePassageReview.Should().BeTrue();
            vm.SegmentRequirePassageListen.Should().BeTrue();
            vm.SegmentRequireSectionListen.Should().BeTrue();
            vm.Segment2IsActive.Should().BeFalse();
            vm.Segment2DoPassageReview.Should().BeFalse();
            vm.Segment2RequirePassageReview.Should().BeFalse();
            vm.Segment2RequirePassageListen.Should().BeFalse();
            vm.Segment2RequireSectionListen.Should().BeFalse();
            vm.Segment2TranscribeIsActive.Should().BeFalse();
            vm.AllowTurnOnSegment2.Should().BeTrue();
        }
        [Fact]
        public void TurnOffRetell2_TurnsOffAllOfRetell2_AndNoneOfRetell1()
        {
            //Arrange
            _backTranslateStep.StepSettings.SetSetting(SettingType.DoRetellBackTranslate, false);
            _transcribeStep.StepSettings.SetSetting(SettingType.DoPassageTranscribe, false);
            var vm = GetViewModel();
            vm.Do2StepBackTranslation = false;
            
            //Act
            vm.RetellIsActive = true;
            vm.DoPassageTranscribeIsActive = true;
            vm.Do2StepBackTranslation = true;
            vm.Retell2IsActive = false;

            //Assert
            vm.RetellIsActive.Should().BeTrue();
            vm.DoPassageTranscribeIsActive.Should().BeTrue();
            vm.RetellDoPassageReview.Should().BeTrue();
            vm.RetellRequirePassageReview.Should().BeTrue();
            vm.RetellRequirePassageListen.Should().BeTrue();
            vm.RetellRequireSectionListen.Should().BeTrue();
            vm.Retell2IsActive.Should().BeFalse();
            vm.Retell2DoPassageReview.Should().BeFalse();
            vm.Retell2RequirePassageReview.Should().BeFalse();
            vm.Retell2RequirePassageListen.Should().BeFalse();
            vm.Retell2RequireSectionListen.Should().BeFalse();
            vm.DoPassage2TranscribeIsActive.Should().BeFalse();
            vm.AllowTurnOnRetell2.Should().BeTrue();
        }


        #region [PassageTranscribe & SegmentTranscribe]

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DoPassageTranscribeIsActive_Changed_RequirePassageTranscribeListenIsActive_IsAlsoChanged(bool value)
        {
            //Arrange
            var vm = GetViewModel();
            vm.DoPassageTranscribeIsActive = !value;
            vm.RequirePassageTranscribeListenIsActive = !value;

            //Act
            vm.DoPassageTranscribeIsActive = value;

            //Assert
            vm.RequirePassageTranscribeListenIsActive.Should().Be(vm.DoPassageTranscribeIsActive);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DoPassage2TranscribeIsActive_Changed_RequirePassage2TranscribeListenIsActive_IsAlsoChanged(bool value)
        {
            //Arrange
            var vm = GetViewModel();
            vm.DoPassage2TranscribeIsActive = !value;
            vm.RequirePassage2TranscribeListenIsActive = !value;

            //Act
            vm.DoPassage2TranscribeIsActive = value;

            //Assert
            vm.RequirePassage2TranscribeListenIsActive.Should().Be(vm.DoPassage2TranscribeIsActive);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SegmentTranscribeIsActive_Changed_RequireSegmentTranscribeListenIsActive_IsAlsoChanged(bool value)
        {
            //Arrange
            var vm = GetViewModel();
            vm.SegmentTranscribeIsActive = !value;
            vm.RequireSegmentTranscribeListenIsActive = !value;

            //Act
            vm.SegmentTranscribeIsActive = value;

            //Assert
            vm.RequireSegmentTranscribeListenIsActive.Should().Be(vm.SegmentTranscribeIsActive);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Segment2TranscribeIsActive_Changed_RequireSegment2TranscribeListenIsActive_IsAlsoChanged(bool value)
        {
            //Arrange
            var vm = GetViewModel();
            vm.Segment2TranscribeIsActive = !value;
            vm.RequireSegment2TranscribeListenIsActive = !value;

            //Act
            vm.Segment2TranscribeIsActive = value;

            //Assert
            vm.RequireSegment2TranscribeListenIsActive.Should().Be(vm.Segment2TranscribeIsActive);
        }

        #endregion
        
    }
}