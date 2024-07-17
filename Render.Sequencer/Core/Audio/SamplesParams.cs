namespace Render.Sequencer.Core.Audio;

internal readonly struct SamplesParams
{
    public readonly int NumberOfBars;

    public readonly int SamplesPerBar;

    public SamplesParams(int numberOfBars, int samplesPerBar) 
    {
        NumberOfBars = numberOfBars;
        SamplesPerBar = samplesPerBar;
    }
}