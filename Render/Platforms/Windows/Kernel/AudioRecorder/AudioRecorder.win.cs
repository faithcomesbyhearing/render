using Render.Models.Audio;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;
using System.Diagnostics;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Render.Utilities;
using Render.Services.AudioServices;

namespace Render.Platforms.Kernel.AudioRecorder
{
	public class AudioRecorder : IAudioRecorder
	{
		private readonly string _defaultFileName = "ARS_recording_{0}.wav";
		private readonly string _defaultTempRecordPath;
		private readonly string _tempAudioDirectory;

        private string _currentTempRecordPath;
        private string _deviceId = string.Empty;
        private bool _isInitialized;
        private bool _isRecording;
		private MediaCapture _mediaCapture;
		private LowLagMediaRecording _mediaRecording;
        private IAudioDeviceMonitor _deviceMonitor;

		public bool IsRecording
        {
            get => _isRecording;
            private set => _isRecording = value;
        }

        public AudioDetails AudioStreamDetails { get; }

        public event EventHandler<string> OnRecordFailed;
        public event EventHandler<string> OnRecordDeviceRestore;
		public event EventHandler<string> OnRecordFinished;

        public AudioRecorder(string tempAudioDirectory, int preferredSampleRate, IAudioDeviceMonitor deviceMonitor)
        {
            _tempAudioDirectory = tempAudioDirectory;
            _deviceMonitor = deviceMonitor;
            _defaultFileName = string.Format(_defaultFileName, Guid.NewGuid());
            _defaultTempRecordPath = Path.Combine(tempAudioDirectory, _defaultFileName);
            _currentTempRecordPath = _defaultTempRecordPath;

            AudioStreamDetails = new AudioDetails
            {
                BitsPerSample = 16,
                ChannelCount = 1,
                SampleRate = preferredSampleRate
            };

			_deviceMonitor.AudioInputDeviceChanged += OnDefaultDeviceChanged;
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

		/// <summary>
		/// Since we don't handle switching between multiple microphones,
		/// any change in DeviceId is considered a microphone restore
		/// </summary>
        private void OnDefaultDeviceChanged(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId) || _deviceId == deviceId)
            {
                return;
            }

            OnRecordDeviceRestore?.Invoke(this, deviceId);
            _isInitialized = false;
        }

        private void SetCurrentDevice()
        {
			try
			{
				_deviceId = _deviceMonitor.GetCurrentInputDeviceId();
			}
			catch (Exception exception)
			{
                RenderLogger.LogInfo(nameof(SetCurrentDevice), new Dictionary<string, string>()
                {
                    { nameof(exception.Message), exception.Message },
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
            _mediaCapture.Dispose();
			_mediaCapture = null;
		}

		public void Dispose()
		{
			OnRecordFailed = null;
			OnRecordDeviceRestore = null;
			OnRecordFinished = null;

			_deviceMonitor.AudioInputDeviceChanged -= OnDefaultDeviceChanged;
			_deviceMonitor = null;

			if (_mediaCapture != null)
			{
				DestroyRecorder();
			}
		}
    }
}