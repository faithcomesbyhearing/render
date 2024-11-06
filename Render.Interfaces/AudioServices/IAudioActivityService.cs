using System.Windows.Input;

namespace Render.Interfaces.AudioServices
{
    public interface IAudioActivityService : IDisposable
    {
        bool IsAudioRecording { get; }

        void SetStopCommand(Func<Task> command, bool isAudioRecording);

        void SetStopCommand(Action command, bool isAudioRecording);

        void SetStopCommand(ICommand command, bool isAudioRecording);

        void Stop();

        void StopRecording();
    }
}