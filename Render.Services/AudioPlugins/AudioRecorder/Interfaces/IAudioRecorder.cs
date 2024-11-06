using Render.Models.Audio;

namespace Render.Services.AudioPlugins.AudioRecorder.Interfaces
{
    public interface IAudioRecorder : IDisposable
    {
        bool IsRecording { get; }

        event EventHandler<string> OnRecordFailed;

        event EventHandler<string> OnRecordFinished;

        event EventHandler<string> OnRecordDeviceRestore;

        AudioDetails AudioStreamDetails { get; }

        string GetAudioFilePath();

        Stream GetAudioFileStream();

        Task<Task<string>> StartRecording(FileStream stream=null);

        Task<string> StopRecording(bool continueProcessing = true);
    }
}