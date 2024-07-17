namespace Render.Sequencer.Contracts.Enums;

public static class FlagDirectionExtensions
{
    public static bool IsNorthDirection(this FlagDirection direction)
    {
        return direction > FlagDirection.West || direction < FlagDirection.East;
    }

    public static bool IsSouthDirection(this FlagDirection direction)
    {
        return direction > FlagDirection.East || direction < FlagDirection.West;
    }
}