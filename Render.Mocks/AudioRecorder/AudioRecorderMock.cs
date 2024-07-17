using System.Timers;
using Render.Models.Audio;
using Render.Mocks.AudioRecorder.AudioToneGenerator;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;

namespace Render.Mocks.AudioRecorder
{
    public class AudioRecorderMock : IAudioRecorder
    {
        private const int ToneLengthMs = 100;

        public event EventHandler<string> OnRecordFailed;
        public event EventHandler<string> OnRecordDeviceRestore;
        public event EventHandler<string> OnRecordFinished;

        private readonly string _defaultFileName = "Mock_ARS_recording_{0}.wav";
        private readonly string _defaultTempRecordPath;

        private readonly System.Timers.Timer _timer;
        private readonly AudioDataChunk _sineToneChunk;

        private int _byteCount;
        private bool _isRecording;
        private bool _isInnerStream;

        private FileStream _fileStream;
        private StreamWriter _streamWriter;
        private BinaryWriter _binaryWriter;

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

        public AudioRecorderMock(string tempAudioDirectory, int preferredSampleRate)
        {
            _defaultFileName = string.Format(_defaultFileName, Guid.NewGuid());
            _defaultTempRecordPath = Path.Combine(tempAudioDirectory, _defaultFileName);

            AudioStreamDetails = new AudioDetails
            {
                ChannelCount = 2,
                BitsPerSample = 16,
                SampleRate = preferredSampleRate,
            };

            _timer = new System.Timers.Timer(ToneLengthMs);
            _timer.Elapsed += AddGeneratedTone;
            _timer.AutoReset = true;

            _sineToneChunk = new AudioDataChunk();
            _sineToneChunk.AddSampleData(
                leftChannelBuffer: SineGenerator.GenerateSine(697.0f, (uint)AudioStreamDetails.SampleRate, ToneLengthMs),
                rightChannelBuffer: SineGenerator.GenerateSine(1209.0f, (uint)AudioStreamDetails.SampleRate, ToneLengthMs));
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
            _streamWriter = new StreamWriter(_fileStream);
            _binaryWriter = new BinaryWriter(_streamWriter.BaseStream, System.Text.Encoding.UTF8);

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
            WriteWavHeader(
                channelCount: AudioStreamDetails.ChannelCount,
                sampleRate: AudioStreamDetails.SampleRate,
                bitsPerSample: AudioStreamDetails.BitsPerSample,
                audioLength: _byteCount);

            IsRecording = false;
            OnRecordFinished?.Invoke(this, _defaultTempRecordPath);

            Reset();
            return _defaultTempRecordPath;
        }

        private void AddGeneratedTone(object sender, ElapsedEventArgs e)
        {
            _byteCount += _sineToneChunk.WaveDataBytes.Length;
            _binaryWriter.Write(_sineToneChunk.WaveDataBytes);
        }

        private void Reset()
        {
            _byteCount = 0;

            if (_isInnerStream)
            {
                _streamWriter?.Close();
                _binaryWriter?.Close();
                _fileStream?.Close();
            }
        }

        private void WriteWavHeader(int channelCount, int sampleRate, int bitsPerSample, int audioLength = -1)
        {
            var blockAlign = (short)(channelCount * (bitsPerSample / 8));
            var averageBytesPerSecond = sampleRate * blockAlign;

            if (_binaryWriter.BaseStream.CanSeek)
            {
                _binaryWriter.Seek(0, SeekOrigin.Begin);
            }

            _binaryWriter.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            if (audioLength > -1)
            {
                _binaryWriter.Write(audioLength - 8);
            }
            else
            {
                _binaryWriter.Write(audioLength);
            }

            _binaryWriter.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
            _binaryWriter.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            _binaryWriter.Write(16);
            _binaryWriter.Write((short)1);
            _binaryWriter.Write((short)channelCount);
            _binaryWriter.Write(sampleRate);
            _binaryWriter.Write(averageBytesPerSecond);
            _binaryWriter.Write(blockAlign);
            _binaryWriter.Write((short)bitsPerSample);
            _binaryWriter.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            _binaryWriter.Write(audioLength - 44);
        }

        public void Dispose()
        {
            StopRecording();
            Reset();
        }
    }
}