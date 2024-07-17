using Render.Services.AudioPlugins.AudioRecorder.Interfaces;

namespace Render.Platforms.Kernel.AudioRecorder
{
    public class AudioRecorderFactory : IAudioRecorderFactory
    {
        public IAudioRecorder Create(string tempDirectory, int sampleRate)
        {
            return new AudioRecorder(tempDirectory, sampleRate);
        }
    }
}