using Render.Services.AudioPlugins.AudioRecorder.Interfaces;
using Render.Services.AudioServices;

namespace Render.Platforms.Kernel.AudioRecorder
{
    public class AudioRecorderFactory : IAudioRecorderFactory
    {
        public IAudioRecorder Create(string tempDirectory, int sampleRate, IAudioDeviceMonitor deviceMonitor)
        {
            return new AudioRecorder(tempDirectory, sampleRate, deviceMonitor);
        }
    }
}