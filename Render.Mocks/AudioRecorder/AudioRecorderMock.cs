using System.Timers;
using Render.Models.Audio;
using Render.Mocks.AudioRecorder.AudioToneGenerator;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;

namespace Render.Mocks.AudioRecorder
{
    public class AudioRecorderMock : IAudioRecorder
    {
        private const int ToneLengthMs = 100;

 #pragma warning disable 0067
		public event EventHandler<string> OnRecordFailed;
        public event EventHandler<string> OnRecordDeviceRestore;
#pragma warning restore 0067

		public event EventHandler<string> OnRecordFinished;

        private readonly string _defaultFileName = "Mock_ARS_recording_{0}.wav";
        private readonly string _defaultTempRecordPath;

        private readonly System.Timers.Timer _timer;
        private readonly AudioDataChunk _sineToneChunk;

        private bool _isRecording;
        private bool _isInnerStream;

        private MemoryStream _recordingStream;

        private FileStream _fileStream;
        private BinaryWriter _binaryWriter;

        public bool IsRecording
        {
            get => _isRecording;
            private set => _isRecording = value;
        }

        public AudioDetails AudioStreamDetails { get; }

        public AudioRecorderMock(string tempAudioDirectory, int preferredSampleRate)
        {
            _defaultFileName = string.Format(_defaultFileName, Guid.NewGuid());
            _defaultTempRecordPath = Path.Combine(tempAudioDirectory, _defaultFileName);

            AudioStreamDetails = new AudioDetails
            {
                ChannelCount = 1,
                BitsPerSample = 16,
                SampleRate = preferredSampleRate,
            };

            _timer = new System.Timers.Timer(ToneLengthMs);
            _timer.Elapsed += AddGeneratedTone;
            _timer.AutoReset = true;

            _sineToneChunk = new AudioDataChunk();
            _sineToneChunk.SetSampleData(SineGenerator.GenerateSine(1209.0f, (uint)AudioStreamDetails.SampleRate, ToneLengthMs));
        }

        public string GetAudioFilePath()
        {
            return _defaultTempRecordPath;
        }

        public Stream GetAudioFileStream()
        {
            return new FileStream(
                path: GetAudioFilePath(),
                mode: FileMode.Open,
                access: FileAccess.Read,
                share: FileShare.ReadWrite);
        }

        public Task<Task<string>> StartRecording(FileStream stream = null)
        {
            Reset();

            IsRecording = true;

            _isInnerStream = stream is null;
            _fileStream = stream ?? new FileStream(_defaultTempRecordPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            _binaryWriter = new BinaryWriter(_fileStream, System.Text.Encoding.UTF8);

            _recordingStream = new MemoryStream();

            _timer.Start();

            return Task.FromResult(Task.FromResult(_defaultTempRecordPath));
        }

        public async Task<string> StopRecording(bool continueProcessing = true)
        {
            if (IsRecording is false) 
            {
                return _defaultTempRecordPath;
            }

            _timer.Stop();

            await Task.Delay(ToneLengthMs);

            var audio = new Audio(Guid.Empty, Guid.Empty, Guid.Empty);

            _recordingStream.Position = 0;
            audio.SetAudio(_recordingStream.ToArray());
            audio.WriteWavHeader();

            _fileStream.Position = 0;
            _fileStream.Write(audio.Data);
            _fileStream.Flush();

            IsRecording = false;
            OnRecordFinished?.Invoke(this, _defaultTempRecordPath);

            Reset();

            return _defaultTempRecordPath;
        }

        private void AddGeneratedTone(object sender, ElapsedEventArgs e)
        {
            _recordingStream.Write(_sineToneChunk.WaveDataBytes);
            _binaryWriter.Write(_sineToneChunk.WaveDataBytes);
        }

        private void Reset()
        {
            _recordingStream?.Close();
            _recordingStream?.Dispose();
            _recordingStream = null;

            _binaryWriter?.Close();
            _binaryWriter?.Dispose();
            _binaryWriter = null;

            if (_isInnerStream)
            {
                _fileStream?.Close();
                _fileStream?.Dispose();
                _fileStream = null;
            }
        }

        public void Dispose()
        {
            Reset();
        }
    }
}