namespace Render.Sequencer.Contracts.Enums;

/// <summary>
/// Describes visual states of WaveformItemView (contains Audio) like 
/// 'optional', 'required' or 'completed'.
/// </summary>
public enum AudioOption
{
    /// <summary>
    /// Nothing to display
    /// </summary>
    Optional,

    /// <summary>
    /// Usually yellow circle
    /// </summary>
    Required,

    /// <summary>
    /// Applies fot states, when audios is completed (Checkmark icon) or re-recorded (ReRecord icon)
    /// </summary>
    Completed
}