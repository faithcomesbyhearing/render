using Render.Interfaces.AudioServices;
using Render.Interfaces.WrappersAndExtensions;
using Render.Models.Audio;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;
using Render.Utilities;

namespace Render.Kernel.WrappersAndExtensions
{
    public class AudioRecorderServiceWrapper : IAudioRecorderServiceWrapper
    {
        private IAudioRecorder _audioRecorder;
        private IAudioActivityService _audioActivityService;
        private Func<int, IAudioRecorder> _recorderFactory;
        private Func<Task> _stopRecordingCommand;

        public bool IsRecording
        {
            get => _audioRecorder.IsRecording;
        }

        public AudioDetails AudioStreamDetails
        {
            get => _audioRecorder?.AudioStreamDetails != null ? new AudioDetails
            {
                BitsPerSample = _audioRecorder.AudioStreamDetails.BitsPerSample,
                ChannelCount = _audioRecorder.AudioStreamDetails.ChannelCount,
                SampleRate = _audioRecorder.AudioStreamDetails.SampleRate,
            } : null;
        }

        public AudioRecorderServiceWrapper(
            IAudioActivityService audioActivityService,
            Func<int, IAudioRecorder> recorderFactory,
            Func<Task> stopRecordingCommand,
            Func<Task> onRecordFailedCommand,
            Func<Task> onRecordDeviceRestoreCommand)
        {
            _recorderFactory = recorderFactory;
            _audioRecorder = recorderFactory.Invoke(48000);
            _audioActivityService = audioActivityService;
            _stopRecordingCommand = stopRecordingCommand;
            _audioRecorder.OnRecordFailed +=
                async (sender, deviceId) => await onRecordFailedCommand();
            _audioRecorder.OnRecordDeviceRestore +=
                async (sender, deviceId) => await onRecordDeviceRestoreCommand();
        }

        public async Task<Task<string>> StartRecording()
        {
            RenderLogger.LogInfo("Start Recording");
            try
            {
                var result = await _audioRecorder.StartRecording();
                _audioActivityService.SetStopCommand(_stopRecordingCommand, true);
                return result;
            }
            catch (Exception e)
            {
                /* Plugin.AudioRecorder throws a new Exception with this text when it cannot
                   create AudioGraph with sample rate 48000 on VM and some devices.*/
                if (string.IsNullOrEmpty(e.Message) ||
                    !e.Message.Equals("AudioGraph.CreateAsync() returned non-Success status: UnknownFailure"))
                {
                    
                    throw;
                }
                _audioRecorder = _recorderFactory.Invoke(44100);
                
                var result = await _audioRecorder.StartRecording();
                _audioActivityService.SetStopCommand(_stopRecordingCommand, true);
                return result;
            }
        }

        public async Task StopRecording()
        {
            RenderLogger.LogInfo("Stop Recording");

            await _audioRecorder.StopRecording();
        }

        public string GetAudioFilePath()
        {
            return _audioRecorder.GetAudioFilePath();
        }

        public Stream GetAudioFileStream()
        {
            return _audioRecorder.GetAudioFileStream();
        }

        public void Dispose()
        {
            _audioRecorder?.Dispose();
            _audioRecorder = null;

            _audioActivityService?.Dispose();
            _audioActivityService = null;

            _recorderFactory = null;
            _stopRecordingCommand = null;
        }
    }
}