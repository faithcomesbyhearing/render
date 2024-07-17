namespace Render.Models.Workflow.Stage
{
    public class PeerCheckStage : Stage
    {
        public static PeerCheckStage Create()
        {
            var stage = new PeerCheckStage()
            {
                StageSettings = new WorkflowSettings(
                    Setting.NoSelfCheck,
                    Setting.LoopByDefault,
                    Setting.TranslatorCanSkipCheck)
            };

            var reviewStep = new Step(RenderStepTypes.PeerCheck, Roles.Review,
                settings: new WorkflowSettings(
                    Setting.IsActive,
                    Setting.DoPassageReview,
                    Setting.RequirePassageReview,
                    Setting.DoSectionReview,
                    Setting.RequireSectionReview,
                    Setting.RequireNoteListen));

            stage.AddReviewStep(reviewStep);

            var reviseStep = new Step(RenderStepTypes.PeerRevise, Roles.Drafting,
                settings: new WorkflowSettings(Setting.IsActive,
                    Setting.DoPassageReview,
                    Setting.RequirePassageReview,
                    Setting.RequireNoteListen,
                    Setting.AllowEditing));
            
            reviseStep.StepSettings.SetSetting(SettingType.AllowEditing, false);
            
            stage.SetReviseStep(reviseStep);
            
            return stage;
        }
        
        private PeerCheckStage() : base(StageTypes.PeerCheck)
        {
            SetName(Stage.PeerCheckDefaultStageName);
        }
    }
}