using Render.Models.Audio;

namespace Render.Interfaces.WrappersAndExtensions
{
    public interface IAudioRecorderServiceWrapper : IDisposable
    {
        bool IsRecording { get; }

        AudioDetails AudioStreamDetails { get; }

        Task<Task<string>> StartRecording();

        Task StopRecording();

        string GetAudioFilePath();

        Stream GetAudioFileStream();
    }
}