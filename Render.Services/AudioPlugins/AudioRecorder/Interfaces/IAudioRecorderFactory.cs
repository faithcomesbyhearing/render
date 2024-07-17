using Render.Models.Audio;

namespace Render.Services.AudioPlugins.AudioRecorder.Interfaces
{
    public interface IAudioRecorderFactory
    {
        IAudioRecorder Create(string tempDirectory, int sampleRate);
    }
}