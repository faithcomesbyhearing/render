namespace Render.Models.Workflow.Stage
{
    public class ConsultantCheckStage : Stage
    {
        public static ConsultantCheckStage Create()
        {
            var stage = new ConsultantCheckStage()
            {
                StageSettings = new WorkflowSettings(
                    new Setting(SettingType.TranslatorCanSkipCheck, false),
                    Setting.LoopByDefault)
            };
            
            var backTranslateMultiStep = new Step(role: Roles.BackTranslate);
            var backTranslateFollowup = new Step(order: Step.Ordering.Parallel);
            var backTranslate2MultiStep = new Step(role: Roles.BackTranslate2);
            
            backTranslateMultiStep.AddStep(new Step(RenderStepTypes.BackTranslate, Roles.BackTranslate,
                settings: WorkflowSettings.StandardBackTranslateSettings));
            backTranslateMultiStep.AddStep(backTranslateFollowup);

            var transcribeStep = new Step(RenderStepTypes.Transcribe, Roles.Transcribe,
                settings: new WorkflowSettings(
                    Setting.IsActive,
                    Setting.DoSegmentTranscribe,
                    new Setting(SettingType.RequirePassageTranscribeListen, false),
                    Setting.DoPassageTranscribe,
                    new Setting(SettingType.RequireSegmentTranscribeListen, false)));
            transcribeStep.StepSettings.SetSetting(SettingType.IsActive, false);
            transcribeStep.StepSettings.SetSetting(SettingType.DoSegmentTranscribe, false);
            transcribeStep.StepSettings.SetSetting(SettingType.RequirePassageTranscribeListen, false);
            transcribeStep.StepSettings.SetSetting(SettingType.DoPassageTranscribe, false);
            transcribeStep.StepSettings.SetSetting(SettingType.RequireSegmentTranscribeListen, false);
            backTranslateFollowup.AddStep(transcribeStep);
            backTranslateFollowup.AddStep(backTranslate2MultiStep);
            
            var backTranslate2 = new Step(RenderStepTypes.BackTranslate, Roles.BackTranslate2,
                settings: WorkflowSettings.StandardBackTranslateSettings);
            backTranslate2.StepSettings.SetSetting(SettingType.IsActive, false);
            backTranslate2.StepSettings.SetSetting(SettingType.DoRetellBackTranslate, false);
            backTranslate2.StepSettings.SetSetting(SettingType.DoSegmentBackTranslate, false);
            
            backTranslate2MultiStep.AddStep(backTranslate2);
           
            var backTranslateTranscribe2 = new Step(RenderStepTypes.Transcribe, Roles.Transcribe2,
                settings: new WorkflowSettings(Setting.IsActive));
            backTranslateTranscribe2.StepSettings.SetSetting(SettingType.IsActive, false);
            
            backTranslate2MultiStep.AddStep(backTranslateTranscribe2);
            
            stage.AddParallelReviewPreparationStep(backTranslateMultiStep);

            var interpretToConsultantStep = new Step(RenderStepTypes.InterpretToConsultant,
                Roles.NoteTranslate,
                settings: new WorkflowSettings(
                    new Setting(SettingType.IsActive, false),
                    new Setting(SettingType.RequireNoteListen, false),
                    new Setting(SettingType.DoNoteReview, false),
                    new Setting(SettingType.RequireNoteReview, false)));
            stage.AddSequentialReviewPreparationStep(interpretToConsultantStep);
            
            stage.AddReviewStep(new Step(RenderStepTypes.ConsultantCheck, 
                Roles.Consultant,
                settings: new WorkflowSettings(
                    new Setting(SettingType.IsActive),
                    new Setting(SettingType.RequireNoteListen))
                ));
            stage.AddRevisePreparationStep(new Step(RenderStepTypes.InterpretToTranslator, Roles.NoteTranslate,
                settings: new WorkflowSettings(
                    new Setting(SettingType.IsActive, false),
                    new Setting(SettingType.RequireNoteListen),
                    new Setting(SettingType.DoNoteReview),
                    new Setting(SettingType.RequireNoteReview))));
            stage.SetReviseStep(new Step(RenderStepTypes.ConsultantRevise, Roles.Drafting,
                settings: new WorkflowSettings(Setting.IsActive,
                    Setting.DoPassageReview,
                    Setting.RequirePassageReview,
                    Setting.RequireNoteListen,
                    Setting.AllowEditing)));
            
            return stage;
        }
        
        private ConsultantCheckStage() : base(StageTypes.ConsultantCheck)
        {
            SetName(Stage.ConsultantCheckDefaultStageName);
        }
    }
}