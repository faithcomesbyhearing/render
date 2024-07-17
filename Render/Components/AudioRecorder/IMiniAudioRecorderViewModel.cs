using System.Reactive;
using ReactiveUI;
using Render.Kernel;
using Render.Models.Audio;
using Render.Services.AudioServices;

namespace Render.Components.AudioRecorder
{
    public interface IMiniAudioRecorderViewModel : IActionViewModelBase
    {
        AudioRecorderState AudioRecorderState { get; }

        Task<Audio> GetAudio();
        void SetAudio(Audio audio);
        float[] AudioSamples { get; set; }
        double RecordedFileDuration { get; set; }
        double CurrentPosition { get; set; }
        string AudioFilePath { get; }

        ReactiveCommand<Unit, Unit> StartRecordingCommand { get; }
        ReactiveCommand<Unit, Unit> StopRecordingCommand { get; }
        ReactiveCommand<Unit, Unit> PlayCommand { get; }
        ReactiveCommand<Unit, Unit> PauseCommand { get; }
        ReactiveCommand<Unit, Unit> DeleteCommand { get; }
        IAudioPlayerService AudioPlayer { get; }

        IObservable<Unit> StopRecorderActivity();
    }
}