using Render.Models.Workflow;
using Render.Resources.Localization;

namespace Render.Kernel.WrappersAndExtensions;

public static class StepExtensions
{
    public static string GetName(this Step step, string defaultStepName)
    {
        return string.IsNullOrWhiteSpace(step.CustomName) ? defaultStepName : step.CustomName;
    }

    public static string GetName(this Step step)
    {
        return string.IsNullOrWhiteSpace(step.CustomName) ? GetName(step.RenderStepType) : step.CustomName;
    }

    private static string GetName(this RenderStepTypes stepType)
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
                return AppResources.NoteInterpretToConsultant;
            case RenderStepTypes.InterpretToTranslator:
                return AppResources.NoteInterpretToTranslator;
            case RenderStepTypes.BackTranslate:
                return AppResources.BackTranslate;
            case RenderStepTypes.NotSpecial:
            case RenderStepTypes.Unknown:
                return "";
            default:
                throw new ArgumentOutOfRangeException(nameof(RenderStepTypes), stepType, null);
        }
    }
}