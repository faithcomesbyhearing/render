namespace Render.Sequencer.Core.Utils.Extensions;

internal static class TimeExtensions
{
    internal static double ToSeconds(this double milliseconds)
    {
        return milliseconds / 1000;
    }

    internal static double ToMilliseconds(this double seconds)
    {
        return seconds * 1000;
    }
}