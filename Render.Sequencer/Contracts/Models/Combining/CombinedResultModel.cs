namespace Render.Sequencer.Contracts.Models.Combining;

public record CombinedResultModel
{
    public double StartTime { get; init; }

    public double EndTime { get; init; }

    public Guid[] CombinedAudiosIds { get; init; }

    public bool HasNewCombinedSegments
    {
        get => CombinedAudiosIds.Length > 1;
    }

    public CombinedResultModel(double startTime, double endTime, Guid[] combinedAudiosIds)
    {
        StartTime = startTime;
        EndTime = endTime;
        CombinedAudiosIds = combinedAudiosIds;
    }
}
