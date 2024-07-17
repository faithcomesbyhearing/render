using Render.Models.Workflow;
using Render.Resources;
using Render.Resources.Styles;

namespace Render.Kernel;

public static class IconMapper
{
    public static FontImageSource GetIconForStageType(StageTypes stageType, bool getAlternateColor = false)
    {
        var color = getAlternateColor
            ? (ColorReference)ResourceExtensions.GetResourceValue("MainIconColor")
            : (ColorReference)ResourceExtensions.GetResourceValue("SecondaryText");
        if (color != null)
        {
            switch (stageType)
            {
                case StageTypes.Generic:
                    return IconExtensions.BuildFontImageSource(Icon.GenericCheck, color.Color);
                case StageTypes.Drafting:
                    return getAlternateColor
                        ? (FontImageSource)ResourceExtensions.GetResourceValue("MicrophoneSource")
                        : (FontImageSource)ResourceExtensions.GetResourceValue("MicrophoneWhite");
                case StageTypes.PeerCheck:
                    return getAlternateColor
                        ? (FontImageSource)ResourceExtensions.GetResourceValue("PeerCheckSource")
                        : (FontImageSource)ResourceExtensions.GetResourceValue("PeerCheckWhite");
                case StageTypes.CommunityTest:
                    return getAlternateColor
                        ? (FontImageSource)ResourceExtensions.GetResourceValue("CommunityTestSource")
                        : (FontImageSource)ResourceExtensions.GetResourceValue("CommunityTestWhite");
                case StageTypes.ConsultantCheck:
                    return getAlternateColor
                        ? (FontImageSource)ResourceExtensions.GetResourceValue("ConsultantCheckOriginalSource")
                        : (FontImageSource)ResourceExtensions.GetResourceValue("ConsultantCheckWhite");
                case StageTypes.ConsultantApproval:
                    return IconExtensions.BuildFontImageSource(Icon.ConsultantApproval, color.Color);
                default:
                    return (FontImageSource)ResourceExtensions.GetResourceValue("PlaceholderWhite");
            }
        }

        return (FontImageSource)ResourceExtensions.GetResourceValue("PlaceholderWhite");
    }

    public static string GetIconGlyphForStepType(RenderStepTypes stepType)
    {
        return (string)ResourceExtensions.GetResourceValue(GetIconNameForStepType(stepType));
    }

    public static string GetIconNameForStepType(RenderStepTypes stepType)
    {
        string glyph;
        switch (stepType)
        {
            case RenderStepTypes.Draft:
                glyph = Icon.Translate.ToString();
                break;
            case RenderStepTypes.PeerCheck:
                glyph = Icon.PeerReview.ToString();
                break;
            case RenderStepTypes.PeerRevise:
                glyph = Icon.PeerRevise.ToString();
                break;
            case RenderStepTypes.CommunitySetup:
                glyph = Icon.CommunityCheckSetup.ToString();
                break;
            case RenderStepTypes.CommunityTest:
                glyph = Icon.CommunityCheck.ToString();
                break;
            case RenderStepTypes.CommunityRevise:
                glyph = Icon.CommunityRevise.ToString();
                break;
            case RenderStepTypes.BackTranslate:
                glyph = Icon.BackTranslate.ToString();
                break;
            case RenderStepTypes.InterpretToConsultant:
                glyph = Icon.InterpretToConsultant.ToString();
                break;
            case RenderStepTypes.InterpretToTranslator:
                glyph = Icon.InterpretToTranslator.ToString();
                break;
            case RenderStepTypes.Transcribe:
                glyph = Icon.Transcribe.ToString();
                break;
            case RenderStepTypes.ConsultantCheck:
                glyph = Icon.ConsultantCheck.ToString();
                break;
            case RenderStepTypes.ConsultantRevise:
                glyph = Icon.ConsultantRevise.ToString();
                break;
            case RenderStepTypes.ConsultantApproval:
                glyph = Icon.ConsultantApproval.ToString();
                break;
            case RenderStepTypes.NotSpecial:
            case RenderStepTypes.HoldingTank:
            default:
                glyph = Icon.Placeholder.ToString();
                break;
        }

        return glyph;
    }

	public static FontImageSource GetStageIconForExportPage(StageTypes stageType)
	{
		return stageType == StageTypes.Drafting ? IconExtensions.BuildFontImageSource(Icon.DraftIconStage) : GetIconForStageType(stageType);
	}
}