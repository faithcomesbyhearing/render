using Render.Models.Sections;

namespace Render.Components.Consultant.ConsultantCheck;

public enum MenuTabType
{
    None,
    Original,
    BackTranslate,
    SegmentBackTranslate,
    BackTranslate2,
    SegmentBackTranslate2
}
public class MenuButtonParameters
{
    public bool IsVisible { get; init; } = true;
    public bool IsEnabled { get; init; } = true;
    public ParentAudioType AudioType { get; init; }
    public MenuTabType MenuTabType { get; init; }
}