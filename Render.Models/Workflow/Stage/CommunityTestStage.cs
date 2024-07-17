namespace Render.Models.Workflow.Stage
{
    public class CommunityTestStage : Stage
    {
        public static CommunityTestStage Create()
        {
            var stage = new CommunityTestStage()
            {
                StageSettings = new WorkflowSettings(
                    Setting.AssignToTranslator,
                    new Setting(SettingType.LoopByDefault, false),
                    Setting.TranslatorCanSkipCheck)
            };
            stage.AddSequentialReviewPreparationStep(new Step(RenderStepTypes.CommunitySetup, Roles.Review));
            stage.AddReviewStep(new Step(RenderStepTypes.CommunityTest, Roles.Review,
                settings: new WorkflowSettings(
                    Setting.IsActive,
                    Setting.DoSectionListen,
                    Setting.RequireSectionListen,
                    Setting.RequirePassageListen,
                    Setting.DoCommunityRetell,
                    Setting.DoCommunityResponse,
                    new Setting(SettingType.RequireQuestionContextListen),
                    new Setting(SettingType.RequireRecordResponse))));

            stage.SetReviseStep(new Step(RenderStepTypes.CommunityRevise, Roles.Drafting,
                settings: new WorkflowSettings(
                    Setting.IsActive,
                    Setting.DoPassageReview,
                    Setting.RequirePassageReview,
                    Setting.RequireSectionListen,
                    new Setting(SettingType.AllowEditing, initialValue: false),
                    new Setting(SettingType.RequireCommunityFeedback))));
            
            return stage;
        }
        
        private CommunityTestStage() : base(StageTypes.CommunityTest)
        {
            SetName(Stage.CommunityTestDefaultStageName);
        }
    }
}