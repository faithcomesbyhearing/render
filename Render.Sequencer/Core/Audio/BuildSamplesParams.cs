namespace Render.Sequencer.Core.Audio;

internal readonly struct BuildSamplesParams
{
    public readonly int BitPerSample;

    public readonly double Duration;

    public readonly double BarsPerSecond;

    public readonly int MaxSecondsOnScreen;

    public readonly double BuildSampleIntervalMs;

    public readonly double VisibleBarsLength;

    public BuildSamplesParams(
        int bitPerSamples,
        double duration,
        double barsPerSecond,
        int maxSecondsOnScreen,
        double buildSampleIntervalMs,
        double visibleBarsLength)
    {
        BitPerSample = bitPerSamples;
        Duration = duration;
        BarsPerSecond = barsPerSecond;
        MaxSecondsOnScreen = maxSecondsOnScreen;
        BuildSampleIntervalMs = buildSampleIntervalMs;
        VisibleBarsLength = visibleBarsLength;
    }
}