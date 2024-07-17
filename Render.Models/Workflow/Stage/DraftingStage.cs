using Newtonsoft.Json;

namespace Render.Models.Workflow.Stage
{
    public class DraftingStage : Stage
    {
        [JsonProperty("Step")] private Step Step { get; set; }

        [JsonIgnore] public override List<Step> Steps => new List<Step> { Step };

        public static DraftingStage Create()
        {
            var draftingStep = new Step(
                renderStepType: RenderStepTypes.Draft, 
                role: Roles.Drafting,
                settings: WorkflowSettings.StandardReviseSettings);

            draftingStep.StepSettings.SetSetting(SettingType.AllowEditing, false);

            var stage = new DraftingStage()
            {
                Step = draftingStep
            };
            return stage;   
        }

        private DraftingStage() : base(StageTypes.Drafting)
        {
            SetName(Stage.DraftingDefaultStageName);
        }
    }
}