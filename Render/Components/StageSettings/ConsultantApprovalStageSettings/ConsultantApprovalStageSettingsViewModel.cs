using Render.Kernel;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;

namespace Render.Components.StageSettings.ConsultantApprovalStageSettings
{
    public class ConsultantApprovalStageSettingsViewModel : StageSettingsViewModelBase
    {
        public StepNameViewModel ConsultantApprovalStepName { get; }

        public ConsultantApprovalStageSettingsViewModel(
            RenderWorkflow workflow,
            Stage stage,
            IViewModelContextProvider viewModelContextProvider,
            Action<Stage> updateStageCard)
            : base(
                renderWorkflow: workflow,
                stage: stage,
                viewModelContextProvider: viewModelContextProvider,
                updateStageCard: updateStageCard)
        {
            ConsultantApprovalStepName = new StepNameViewModel(Stage.Steps.First());
        }

        protected override void UpdateWorkflow()
        {
            ConsultantApprovalStepName.UpdateEntity();
            base.UpdateWorkflow();
        }
    }
}