namespace Render.Sequencer.Core.Utils.Helpers;

internal class TapHelper
{
    /// <summary>
    /// Slider doesn't provide tap callback by default. 
    /// Instead of this DragStarted and DragCompleted events fire.
    /// Need to determine whether the user tapping or dragging on the waveform by position delta.
    /// </summary>
    internal static bool IsTapped(double previousPosition, double currentPosition)
    {
        if (previousPosition == currentPosition)
        {
            return true;
        }

        const double minDragDelta = 0.1;
        return Math.Abs(previousPosition - currentPosition) > minDragDelta;
    }
}