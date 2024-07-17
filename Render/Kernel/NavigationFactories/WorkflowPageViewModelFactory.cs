using Render.Models.Sections;
using Render.Models.Workflow;

namespace Render.Kernel.NavigationFactories
{
    public static class WorkflowPageViewModelFactory
    {
        public static async Task<ViewModelBase> GetViewModelToNavigateTo(IViewModelContextProvider viewModelContextProvider, 
            Step step, Section section)
        {
            IStepViewModelDispatcher dispatcher = null;
                switch (step.RenderStepType)
            {
                case RenderStepTypes.NotSpecial:
                    break;
                case RenderStepTypes.Draft:
                    dispatcher = new DraftingDispatcher();
                    break;
                case RenderStepTypes.PeerCheck:
                    dispatcher = new PeerCheckDispatcher();
                    break;
                case RenderStepTypes.PeerRevise:
                    dispatcher = new ReviseDispatcher();
                    break;
                case RenderStepTypes.CommunitySetup:
                    dispatcher = new CommunitySetupDispatcher();
                    break;
                case RenderStepTypes.CommunityTest:
                    dispatcher = new CommunityTestDispatcher();
                    break;
                case RenderStepTypes.CommunityRevise:
                    dispatcher = new CommunityReviseDispatcher();
                    break;
                case RenderStepTypes.BackTranslate:
                    dispatcher = new BackTranslateDispatcher();
                    break;
                case RenderStepTypes.InterpretToConsultant:
                    dispatcher = new NoteInterpretDispatcher();
                    break;
                case RenderStepTypes.InterpretToTranslator:
                    dispatcher = new NoteInterpretDispatcher();
                    break;
                case RenderStepTypes.Transcribe:
                    dispatcher = new TranscribeBackTranslateDispatcher();
                    break;
                case RenderStepTypes.ConsultantCheck:
                    break;
                case RenderStepTypes.ConsultantRevise:
                    dispatcher = new ReviseDispatcher();
                    break;
                case RenderStepTypes.ConsultantApproval:
                case RenderStepTypes.HoldingTank:
                default:
                    throw new ArgumentOutOfRangeException(nameof(step), step, null);
            }

            if (dispatcher == null)
            {
                return null;
            }

            return await dispatcher.GetViewModelToNavigateTo(viewModelContextProvider, step, section);
        }
    }
}