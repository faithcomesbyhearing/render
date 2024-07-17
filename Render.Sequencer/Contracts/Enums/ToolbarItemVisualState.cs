namespace Render.Sequencer.Contracts.Enums;

/// <summary>
/// Describes all toolbar item (button) visual state,
/// based on ToolbarItemState and ToolbarItemOption.
/// </summary>
public enum ToolbarItemVisualState
{
    ActiveOptional,
    ActiveRequired,
    Disabled,
    ToggledOptional,
    ToggledRequired,
}