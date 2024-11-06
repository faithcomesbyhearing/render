using Render.Interfaces.AudioServices;
using System.Windows.Input;
using Render.Interfaces;

namespace Render.Services.AudioServices
{
    /// <summary>
    /// Allows to stop any audio playback/recording that is currently being played/recorded.
    /// </summary>
    public class AudioActivityService : IAudioActivityService
    {
        private object _stopCommand;
        private bool _isAudioRecording;
        private readonly IRenderLogger _renderLogger;

        public bool IsAudioRecording 
        { 
            get => _isAudioRecording; 
        }

        public AudioActivityService(IRenderLogger renderLogger)
        {
            _renderLogger = renderLogger;
        }

        public void SetStopCommand(Func<Task> command, bool isAudioRecording) => SetStopCommand((object)command, isAudioRecording);

        public void SetStopCommand(Action command, bool isAudioRecording) => SetStopCommand((object)command, isAudioRecording);

        public void SetStopCommand(ICommand command, bool isAudioRecording) => SetStopCommand((object)command, isAudioRecording);

        public void Stop()
        {
            switch (_stopCommand)
            {
                case Action stopCommand:
                    stopCommand.Invoke();
                    break;
                case Func<Task> stopCommandAsync:
                    FireAndForget(stopCommandAsync);
                    break;
                case ICommand stopCommand:
                    stopCommand.Execute(null);
                    break;
            }

            _stopCommand = default;
            _isAudioRecording = default;
        }

        public void StopRecording()
        {
            if (_isAudioRecording)
            {
                Stop();
            }
        }

        private void SetStopCommand(object command, bool isAudioRecording)
        {
            if (command == default)
            {
                throw new ArgumentException(nameof(command));
            }

            // Stop previous audio activity (playback/recording) when the command differs from the current _stopCommand instance.
            // Otherwise, that means that audio is playing in the same player and should continue to play. 
            if (_stopCommand != command)
            {
                Stop(); 
            }

            _stopCommand = command;
            _isAudioRecording = isAudioRecording;
        }
        
        private async void FireAndForget(Func<Task> asyncAction)
        {
            try
            {
                var task = asyncAction.Invoke();
                await task.ConfigureAwait(true);
            }
            catch (Exception e)
            {
                _renderLogger.LogError(e);
                throw;
            }
        }

        public void Dispose()
        {
            _stopCommand = null;
        }
    }
}