using Render.Sequencer.Views.Flags;

namespace Render.Sequencer.Contracts.Enums;

public static class FlagDirectionHelper
{
    private struct DirectionParams
    {
        public static DirectionParams Default(double position) => new(position, FlagDirection.North, FlagDirection.South);
        public static DirectionParams LeftEdge(double position) => new(position, FlagDirection.NorthEast, FlagDirection.SouthEast);
        public static DirectionParams RightEdge(double position) => new(position, FlagDirection.NorthWest, FlagDirection.SouthWest);

        public double Position { get; init; }

        public FlagDirection NorthDirection { get; init; }

        public FlagDirection SouthDirection { get; init; }

        public DirectionParams(double position, FlagDirection northDirection, FlagDirection southDirection)
        {
            Position = position;
            NorthDirection = northDirection;
            SouthDirection = southDirection;
        }
    }

    public static void UpdateFlagsDirection(NoteFlagViewModel[] noteFlags, double waveformWidth)
    {
        var nearestFlags = new List<NoteFlagViewModel>(2);
        for (int i = 0; i < noteFlags.Length; i++)
        {
            nearestFlags.Clear();

            var currentFlag = noteFlags[i];
            var previousFlag = i > 0 ? noteFlags[i - 1] : null;
            var nextFlag = i < noteFlags.Length - 2 ? noteFlags[i + 1] : null;
            if (previousFlag is not null)
            {
                nearestFlags.Add(previousFlag);
            }
            if (nextFlag is not null)
            {
                nearestFlags.Add(nextFlag);
            }

            currentFlag.Direction = GetFlagDirection(
                flags: nearestFlags,
                position: currentFlag.PositionDip,
                width: waveformWidth);
        }
    }

    /// <summary>
    /// Evaluetes circular flag direction to avoid flags overlapping. 
    /// </summary>
    /// <param name="flags">Collection of flags, without current flag for which the direction is evaluated.</param>
    /// <param name="position">Flag position in DIP</param>
    /// <param name="width">Waveform width in DIP</param>
    /// <returns></returns>
    public static FlagDirection GetFlagDirection(IEnumerable<NoteFlagViewModel> flags, double position, double width)
    {
        var directionParams = DirectionParams.Default(position);

        if (IsNearLeftEdge(position))
        {
            directionParams = DirectionParams.LeftEdge(position);
        }
        else if (IsNearRightEdge(position, width))
        {
            directionParams = DirectionParams.RightEdge(position);
        }

        return GetDirection(directionParams, flags);
    }

    private static bool IsNearLeftEdge(double position)
    {
        return position <= NoteFlagView.DefaultWidth;
    }

    private static bool IsNearRightEdge(double position, double width)
    {
        return position >= width - NoteFlagView.DefaultWidth;
    }

    private static FlagDirection GetDirection(DirectionParams parameters, IEnumerable<NoteFlagViewModel> flags)
    {
        var newFlagDirection = parameters.NorthDirection;
        var flagsNearBy = GetNearestFlag(parameters.Position, flags);

        if (flagsNearBy is not null)
        {
            var isNorthDirection = flagsNearBy.Direction.IsNorthDirection();
            newFlagDirection = isNorthDirection ? parameters.SouthDirection : parameters.NorthDirection;
        }

        return newFlagDirection;
    }

    private static NoteFlagViewModel? GetNearestFlag(double position, IEnumerable<NoteFlagViewModel> flags)
    {
        return flags
            .Select(flag => new { Flag = flag, Offset = Math.Abs(flag.PositionDip - position) })
            .Where(flag => flag.Offset <= NoteFlagView.DefaultWidth)
            .OrderBy(flag => flag.Offset)
            .FirstOrDefault()?.Flag;
    }

    /// <summary>
    /// Update marker flag position to avoid flags overlapping. 
    /// </summary>
    /// <param name="position">Flag position in DIP</param>
    /// <param name="width">Waveform width in DIP</param>
    /// <returns></returns>
    public static double GetFlagPosition(double position, double width)
    {
        if (IsNearxLeftEdge(position))
        {
            position = (position + MarkerFlagView.HalfOfMarkerWidth);
        }
        else if (IsNearxRightEdge(position, width))
        {
            position = position - MarkerFlagView.HalfOfMarkerWidth/2;
        }

        return position;
    }

    private static bool IsNearxLeftEdge(double position)
    {
        return position == 0;
    }

    private static bool IsNearxRightEdge(double position, double width)
    {
        return position >= width - MarkerFlagView.DefaultWidth;
    }
}