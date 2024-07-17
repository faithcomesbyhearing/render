using Render.Models.Sections;

namespace Render.Components.Consultant.ConsultantCheck;

public class MenuButtonParameters
{
    public bool IsVisible { get; init; } = true;
    public bool IsEnabled { get; init; } = true;
    public bool IsBackTranslate { get; init; }
    public bool IsSegmentBackTranslate { get; init; }
    public bool IsSecondStepBackTranslate { get; init; }
    public ParentAudioType AudioType { get; init; }

}