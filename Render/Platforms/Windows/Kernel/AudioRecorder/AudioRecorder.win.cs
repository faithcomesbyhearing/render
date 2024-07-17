using Render.Models.Audio;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;
using System.Diagnostics;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using CSCore.CoreAudioAPI;
using Render.Utilities;

namespace Render.Platforms.Kernel.AudioRecorder
{
	public class AudioRecorder : IAudioRecorder
	{
		private readonly string _defaultFileName = "ARS_recording_{0}.wav";
		private readonly string _defaultTempRecordPath;
		private readonly string _tempAudioDirectory;

		private readonly MMDeviceEnumerator _deviceEnumerator;
		private readonly MMNotificationClient _notificationClient;

        private string _currentTempRecordPath;
        private string _deviceId = string.Empty;
        private bool _isInitialized;
        private bool _isRecording;
		private MediaCapture _mediaCapture;
		private LowLagMediaRecording _mediaRecording;

		public bool IsRecording 
		{ 
			get => _isRecording; 
			private set 
			{
				_isRecording = value;
				IAudioRecorder.IsRecordingChanged?.Invoke(value);
			}
		}

		public AudioDetails AudioStreamDetails { get; }

        public event EventHandler<string> OnRecordFailed;
        public event EventHandler<string> OnRecordDeviceRestore;
		public event EventHandler<string> OnRecordFinished;

        public AudioRecorder(string tempAudioDirectory, int preferredSampleRate)
        {
            _tempAudioDirectory = tempAudioDirectory;
            _defaultFileName = string.Format(_defaultFileName, Guid.NewGuid());
            _defaultTempRecordPath = Path.Combine(tempAudioDirectory, _defaultFileName);
            _currentTempRecordPath = _defaultTempRecordPath;

            AudioStreamDetails = new AudioDetails
            {
                BitsPerSample = 16,
                ChannelCount = 1,
                SampleRate = preferredSampleRate
            };

            _deviceEnumerator = new MMDeviceEnumerator();
            _notificationClient = new MMNotificationClient(_deviceEnumerator);
			_notificationClient.DefaultDeviceChanged += OnDefaultDeviceChanged;
        }

        public string GetAudioFilePath()
        {
            return _currentTempRecordPath;
        }

		public Stream GetAudioFileStream()
		{
			return new FileStream(
				path: GetAudioFilePath(),
				mode: FileMode.Open,
				access: FileAccess.Read,
				share: FileShare.ReadWrite);
		}

        public async Task<Task<string>> StartRecording(FileStream stream=null)
        {
            if (IsRecording)
            {
                return Task.FromResult(GetAudioFilePath());
            }

            _currentTempRecordPath = stream?.Name ?? _defaultTempRecordPath;

            if (_isInitialized is false)
            {
                SetCurrentDevice();
                await InitializeRecorderAsync();
            }

            _mediaRecording = stream is null ? 

                await _mediaCapture.PrepareLowLagRecordToStorageFileAsync(
                    encodingProfile: CreateEncodingProfile(), 
                    file: await CreateTempRecordFile()) : 

                await _mediaCapture.PrepareLowLagRecordToStreamAsync(
                    encodingProfile: CreateEncodingProfile(),
                    stream: stream.AsRandomAccessStream());

            await _mediaRecording.StartAsync();

            IsRecording = true;

            return Task.FromResult(GetAudioFilePath());
        }

		public async Task<string> StopRecording(bool continueProcessing = true)
		{
			var path = GetAudioFilePath();

			if (IsRecording is false || _mediaRecording is null)
			{
				return path;
			}

			IsRecording = false;

			await _mediaRecording.StopAsync();
			await _mediaRecording.FinishAsync();

			_mediaRecording = null;

			OnRecordFinished?.Invoke(this, path);

			return path;
		}

		private async Task InitializeRecorderAsync()
		{
			_mediaCapture = new MediaCapture();
			_mediaCapture.Failed += MediaCaptureFailed;

            await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
			{
				MediaCategory = MediaCategory.Media,
				StreamingCaptureMode = StreamingCaptureMode.Audio,
				AudioProcessing = AudioProcessing.Default,
			});

            _isInitialized = true;
        }

        private MediaEncodingProfile CreateEncodingProfile()
		{
			var mediaProfile = MediaEncodingProfile.CreateWav(AudioEncodingQuality.Auto);
			mediaProfile.Audio = AudioEncodingProperties.CreatePcm(
				sampleRate: (uint)AudioStreamDetails.SampleRate,
				channelCount: (uint)AudioStreamDetails.ChannelCount,
				bitsPerSample: (uint)AudioStreamDetails.BitsPerSample);

			return mediaProfile;
		}

		private async Task<StorageFile> CreateTempRecordFile()
		{
			var tempRecordFolder = await StorageFolder.GetFolderFromPathAsync(_tempAudioDirectory);

			return await tempRecordFolder.CreateFileAsync(
				desiredName: _defaultFileName,
				options: CreationCollisionOption.ReplaceExisting);
		}

		private void MediaCaptureFailed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
		{
			try
			{
				SetCurrentDevice();
				OnRecordFailed?.Invoke(this, _deviceId);

                Debug.Assert(false, errorEventArgs.Message);

                RenderLogger.LogInfo(nameof(MediaCaptureFailed), new Dictionary<string, string>()
				{
					{ nameof(errorEventArgs.Code), errorEventArgs.Code.ToString() },
					{ nameof(errorEventArgs.Message), errorEventArgs.Message },
				});
			}
			catch(Exception ex)
			{
                RenderLogger.LogInfo(nameof(MediaCaptureFailed), new Dictionary<string, string>()
                {
                    { nameof(ex.Message), ex.Message },
                });
            }

            _isInitialized = false;
        }

        private void OnDefaultDeviceChanged(object s, DefaultDeviceChangedEventArgs eventArgs)
        {
			if (eventArgs.DataFlow == DataFlow.Capture
                && eventArgs.Role == Role.Multimedia
                && !string.IsNullOrEmpty(eventArgs.DeviceId)
                && _deviceId != eventArgs.DeviceId)
            {
                // Since we don't handle switching between multiple microphones,
                // any change in DeviceId is considered a microphone restore
                OnRecordDeviceRestore?.Invoke(this, eventArgs.DeviceId);
                
                _isInitialized = false;
            }
        }

        private void SetCurrentDevice()
        {
			try
			{
				var hasActiveDevice = _deviceEnumerator
					.EnumAudioEndpoints(DataFlow.Capture, DeviceState.Active)
					.Any();

				_deviceId = hasActiveDevice
					? _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia)?.DeviceID
                    : string.Empty;
			}
			catch(Exception ex)
			{
                RenderLogger.LogInfo(nameof(SetCurrentDevice), new Dictionary<string, string>()
                {
                    { nameof(ex.Message), ex.Message },
                });
            }
        }

        private async void DestroyRecorder()
		{
			if (_mediaCapture is null)
			{
				return;
			}

			_isInitialized = false;
			await StopRecording();

			_mediaCapture.Failed -= MediaCaptureFailed;
			_notificationClient.Dispose();
            _mediaCapture.Dispose();
			_mediaCapture = null;
		}

		public void Dispose()
		{
			OnRecordFailed = null;
			OnRecordDeviceRestore = null;
			OnRecordFinished = null;

			_notificationClient.DefaultDeviceChanged -= OnDefaultDeviceChanged;
			
			_deviceEnumerator.Dispose();
			_notificationClient.Dispose();

			if (_mediaCapture != null)
			{
				DestroyRecorder();
			}
		}
    }
}