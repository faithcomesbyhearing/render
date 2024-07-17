using Render.Models.Workflow;
using Render.Resources.Localization;

namespace Render.Utilities
{
    public static class StageTypes
    {
        public static string ToString(Models.Workflow.StageTypes stageType)
        {
            switch (stageType)
            {
                case Models.Workflow.StageTypes.Generic:
                    return AppResources.Generic;
                case Models.Workflow.StageTypes.Drafting:
                    return AppResources.Draft;
                case Models.Workflow.StageTypes.PeerCheck:
                    return AppResources.PeerCheck;
                case Models.Workflow.StageTypes.CommunityTest:
                    return AppResources.CommunityTest;
                case Models.Workflow.StageTypes.ConsultantCheck:
                    return AppResources.ConsultantCheck;
                case Models.Workflow.StageTypes.ConsultantApproval:
                    return AppResources.ConsultantApproval;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stageType), stageType, null);
            }
        }
    }
    
    public static class StepTypes
    {
        public static string ToString(this RenderStepTypes stepType)
        {
            switch (stepType)
            {
                case RenderStepTypes.Draft:
                    return AppResources.Draft;
                case RenderStepTypes.CommunityTest:
                    return AppResources.CommunityTest;
                case RenderStepTypes.CommunityRevise:
                    return AppResources.CommunityRevise;
                case RenderStepTypes.CommunitySetup:
                    return AppResources.CommunitySetup; 
                case RenderStepTypes.ConsultantApproval:
                    return AppResources.ConsultantApproval;  
                case RenderStepTypes.ConsultantCheck:
                    return AppResources.ConsultantCheck;  
                case RenderStepTypes.ConsultantRevise:
                    return AppResources.ConsultantRevise;  
                case RenderStepTypes.HoldingTank:
                    return "";  
                case RenderStepTypes.PeerCheck:
                    return AppResources.PeerCheck;  
                case RenderStepTypes.PeerRevise:
                    return AppResources.PeerRevise;  
                case RenderStepTypes.Transcribe:
                    return AppResources.Transcribe;
                case RenderStepTypes.InterpretToConsultant:
                    return AppResources.InterpretToConsultant;  
                case RenderStepTypes.InterpretToTranslator:
                    return AppResources.InterpretToTranslator;  
                case RenderStepTypes.BackTranslate:
                    return AppResources.BackTranslate;
                case RenderStepTypes.NotSpecial:
                    return "";
                default:
                    throw new ArgumentOutOfRangeException(nameof(RenderStepTypes), stepType, null);
            }
        }
    }
}