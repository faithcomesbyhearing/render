namespace Render.Sequencer.Core.Utils.Streams;

public record RecordedData(
    byte[] AudioData,
    double ChunkDurationSec,
    double TotalDurationSec);