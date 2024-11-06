using Render.Kernel;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.AppStart.Home.NavigationIcons.Workflow;

namespace Render.Pages.AppStart.Home.NavigationIcons;

public static class WorkflowNavigationIconViewModelMapper
    {
        public static WorkflowNavigationIconViewModel GetNavigationIconForStepType(
            IViewModelContextProvider viewModelContextProvider,
            Stage stage,
            Step step,
            int sectionsAtStep,
            Guid projectId)
        {
            switch (step.RenderStepType)
            {
                case RenderStepTypes.ConsultantApproval:
                    return new ConsultantApprovalNavigationIconViewModel(viewModelContextProvider, stage, step, sectionsAtStep);
                case RenderStepTypes.ConsultantCheck:
                    return new ConsultantCheckNavigationIconViewModel(viewModelContextProvider, stage, step, 
                        projectId, sectionsAtStep);
                case RenderStepTypes.CommunitySetup:
                case RenderStepTypes.CommunityTest:
                case RenderStepTypes.CommunityRevise:
                    return new CommunityTestNavigationIconViewModel(viewModelContextProvider, stage, step, sectionsAtStep);
                case RenderStepTypes.Draft:
                case RenderStepTypes.PeerCheck:
                    return new ReferencesWorkflowNavigationIconViewModel(viewModelContextProvider, stage, step, sectionsAtStep);
                case RenderStepTypes.InterpretToConsultant:
                case RenderStepTypes.InterpretToTranslator:
                    return new InterpretNavigationIconViewModel(viewModelContextProvider, stage, step, sectionsAtStep);
                case RenderStepTypes.BackTranslate:
                    return new BackTranslateNavigationIconViewModel(viewModelContextProvider, stage, step, sectionsAtStep);
                case RenderStepTypes.PeerRevise:
                    return new PeerReviseNavigationIconViewModel(viewModelContextProvider, stage, step,sectionsAtStep);
                case RenderStepTypes.NotSpecial:
                case RenderStepTypes.Transcribe:
                case RenderStepTypes.ConsultantRevise:
                case RenderStepTypes.HoldingTank:
                    return new WorkflowNavigationIconViewModel(viewModelContextProvider, stage, step, sectionsAtStep);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }