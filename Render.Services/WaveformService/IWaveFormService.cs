namespace Render.Services.WaveformService
{
    public interface IWaveFormService
    {
        float[] GetEmptyMiniWaveformBars();

        float[] GetMiniWaveformBars(Stream wavStream);

        Task<float[]> GetMiniWaveformBarsAsync(Stream wavStream);

        float[] ConvertAudioDataToFloat(byte[] audioData, int bitPerSample);

        float[] CreateBars(float[] audioData, int numberOfBars, int samplesPerBar);

        float[] InterpolateBars(float[] bars, int requiredCount = 100);
    }
}