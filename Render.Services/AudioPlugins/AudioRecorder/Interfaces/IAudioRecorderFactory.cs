using Render.Services.AudioServices;

namespace Render.Services.AudioPlugins.AudioRecorder.Interfaces
{
    public interface IAudioRecorderFactory
    {
        IAudioRecorder Create(string tempDirectory, int sampleRate, IAudioDeviceMonitor deviceMonitor);
    }
}