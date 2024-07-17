using ReactiveUI;
using Render.Models.Audio;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;
using System.Reactive;

namespace Render.Platforms.Kernel.AudioRecorder
{
    public class AudioRecorder : IAudioRecorder
    {
        public AudioRecorder(string tempAudioDirectory, int preferredSampleRate) { }

        public bool IsRecording { get => throw new NotImplementedException(); }

        public AudioDetails AudioStreamDetails { get => throw new NotImplementedException(); }

        public event EventHandler<string> OnRecordFailed;
        public event EventHandler<string> OnRecordDeviceRestore;

        public string GetAudioFilePath()
        {
            throw new NotImplementedException();
        }

        public Stream GetAudioFileStream()
        {
            throw new NotImplementedException();
        }

        public Task<Task<string>> StartRecording()
        {
            throw new NotImplementedException();
        }

        public Task StopRecording(bool continueProcessing = true)
        {
            throw new NotImplementedException();
        }
    }
}