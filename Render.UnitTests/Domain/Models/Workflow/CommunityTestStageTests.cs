using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Domain.Models.Workflow
{
    public class CommunityTestStageTests : TestBase
    {
        [Fact]
        public void Create_StageSettings_OnlyExpectedSettingTypes()
        {
            //Arrange
            //Act
            var stageSettings = CommunityTestStage.Create().StageSettings;

            //Assert
             var expectedSettings = new List<SettingType>
             {
                 SettingType.AssignToTranslator,
                 SettingType.LoopByDefault,
                 SettingType.TranslatorCanSkipCheck
             };

            stageSettings.Settings.Select(x => x.SettingType)
                .Intersect(expectedSettings)
                .Count()
                .Should()
                .Be(expectedSettings.Count);
        }
        
        [Fact]
        public void Create_ReviewStep_OnlyExpectedSettingTypes()
        {
            //Arrange
            //Act
            var reviewStep = CommunityTestStage.Create().Steps.Single(x => x.RenderStepType == RenderStepTypes.CommunityTest);

            //Assert
            var expectedSettings = new List<SettingType>
            {
                SettingType.IsActive,
                SettingType.DoSectionListen,
                SettingType.RequireSectionListen,
                SettingType.RequirePassageListen,
                SettingType.DoCommunityRetell,
                SettingType.DoCommunityResponse,
                SettingType.RequireQuestionContextListen,
                SettingType.RequireRecordResponse,
            };

            reviewStep.StepSettings.Settings.Select(x => x.SettingType)
                .Intersect(expectedSettings)
                .Count()
                .Should()
                .Be(expectedSettings.Count);
        }
        
        [Fact]
        public void Create_ReviseStep_OnlyExpectedSettingTypes()
        {
            //Arrange
            //Act
            var reviseStep = CommunityTestStage.Create().ReviseStep;

            //Assert
            var expectedSettings = new List<SettingType>
            {
                SettingType.IsActive,
                SettingType.RequireSectionListen,
                SettingType.AllowEditing,
                SettingType.RequireCommunityFeedback,
            };

            reviseStep.StepSettings.Settings.Select(x => x.SettingType)
                .Intersect(expectedSettings)
                .Count()
                .Should()
                .Be(expectedSettings.Count);
        }

        [Fact]
        public void Create_ReviseStep_AllowEditingIsFalseByDefault()
        {
            //Arrange
            //Act
            var reviseStep = CommunityTestStage.Create().ReviseStep;

            //Assert
            reviseStep.StepSettings.Settings.Single(x => x.SettingType == SettingType.AllowEditing).Value.Should().Be(false);
        }
    }
}