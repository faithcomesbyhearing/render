namespace Render.Sequencer.Core.Audio;

internal record AudioSamples
{
    public static AudioSamples CreateEmpty()
    {
        return new AudioSamples(Array.Empty<float>(), Array.Empty<float>());
    }

    public static AudioSamples CreateTotal(float[] samples)
    {
        return new AudioSamples(samples, samples);
    }

    public float[] LastSamples { get; }

    public float[] TotalSamples { get; }

    public AudioSamples(float[] lastSamples, float[] totalSamples)
    {
        LastSamples = lastSamples;
        TotalSamples = totalSamples;
    }
}
