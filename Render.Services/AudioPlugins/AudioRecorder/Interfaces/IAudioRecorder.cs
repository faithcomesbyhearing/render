using Render.Models.Audio;

namespace Render.Services.AudioPlugins.AudioRecorder.Interfaces
{
    public interface IAudioRecorder : IDisposable
    {
        /// <summary>
        /// This code is awful workaround for the BUG 26272.
        /// Will be fixed in the scope of the PBI 26566.
        /// </summary>
        static Action<bool> IsRecordingChanged;

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