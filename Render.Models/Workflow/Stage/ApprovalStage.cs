using Newtonsoft.Json;

namespace Render.Models.Workflow.Stage
{
    public class ApprovalStage : Stage
    {
        [JsonProperty("Step")]
        private Step Step { get; set; }

        [JsonIgnore] public override List<Step> Steps => new List<Step> {Step};

        public static ApprovalStage Create()
        {
            return new ApprovalStage()
            {
                Step = new Step(RenderStepTypes.ConsultantApproval, Roles.Approval),
                StageSettings = new WorkflowSettings(Setting.AssignToConsultant)
            };
        }

        private ApprovalStage() : base(StageTypes.ConsultantApproval)
        {
            SetName(Stage.ConsultantApprovalDefaultStageName);
        }
    }
}