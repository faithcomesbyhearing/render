using Render.Models.Audio;
using Render.Sequencer.Core.Utils.Extensions;

namespace Render.Sequencer.Core.Utils.Streams;

/// <summary>
/// Custom FileStream class that is used by AudioRecorder
/// to write audio data and push them to the client.
/// Currently, this class is used only to draw wave forms, 
/// therefore we don't care about audio data precision.
/// But pushed audio data must have precise length for integer conversion
/// between digital audio signal (PCM) and visual representation (waveform bars).
/// Instance of this class must be used for one time recording only.
/// </summary>
public class ObservableRecordFileStream : FileStream
{
    private readonly int _bytesPerSample;
    private readonly int _sampleRateToChannel;

    private bool _disposed;

    /// <summary>
    /// Chunk duration in milliseconds to have precise integer conversion between
    /// audio data and visual representation (waveform bars)
    /// </summary>
    private readonly double _chunkDurationSec;

    /// <summary>
    /// In memory buffer stream that is used for collecting precise audio data length
    /// before push them to the client. Capacity of the stream is based on <see cref="_chunkDurationSec"/>
    /// </summary>
    private MemoryStream _memoryBuffer;

    /// <summary>
    /// Total recorded audio duration is seconds
    /// </summary>
    private double _totalDurationSec;

    /// <summary>
    /// Estimated value of audio data in chunk that is stored in <see cref="_memoryBuffer"/>
    /// Used only for stale data.
    /// </summary>
    private double _bufferedChunkDurationSec;

    public event Action<RecordedData>? AudioDataRecorded;

    /// <param name="path">Path to audio file to create or open</param>
    /// <param name="audioDetails">Digital audio signal details</param>
    /// <param name="chunkDurationMs">
    /// Chunk duration in milliseconds to have precise integer conversion between
    /// audio data and visual representation (waveform bars)
    /// </param>
    public ObservableRecordFileStream(string path, AudioDetails audioDetails, double chunkDurationMs)
        : base(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite)
    {
        _bytesPerSample = audioDetails.BitsPerSample / 8;
        _sampleRateToChannel = audioDetails.SampleRate * audioDetails.ChannelCount;
        _chunkDurationSec = chunkDurationMs.ToSeconds();

        var totalSamples = (int)(_chunkDurationSec * audioDetails.SampleRate);
        var bufferCapacity = totalSamples * audioDetails.ChannelCount * _bytesPerSample;

        _memoryBuffer = new MemoryStream(bufferCapacity);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        var totalSamples = count / _bytesPerSample;
        var estimatedChunkDurationSec = (double)totalSamples / _sampleRateToChannel;

        _totalDurationSec += estimatedChunkDurationSec;
        _bufferedChunkDurationSec += estimatedChunkDurationSec;

        if (IsAudioData())
        {
            var unfitAudioData = WriteAudioDataToMemoryBuffer(buffer, offset, count);

            if (IsFullMemoryBuffer())
            {
                OnAudioDataRecorded(_memoryBuffer.GetBuffer(), _chunkDurationSec);
                ResetMemoryBuffer(unfitAudioData);
            }
        }

        base.Write(buffer, offset, count);

        /// <summary>
        /// Usualy first ~50-100 bytes of audio file contain header data and other extension subchunks.
        /// Therefore, we assume, if current position is less than ~100 we are writting extension data, otherwise, audio data.
        /// Currently we don't care about audio data precision, because we use them only for visual representation.
        /// </summary>    
        bool IsAudioData()
        {
            return Position > 100;
        }

        bool IsFullMemoryBuffer()
        {
            return _memoryBuffer.Position == _memoryBuffer.Capacity;
        }
    }

    internal void RecordFinished(object? _, string path)
    {
        if (path == Name && HasStaleData())
        {
            OnAudioDataRecorded(
                chunkDurationSec: _bufferedChunkDurationSec,
                audioData: _memoryBuffer
                    .GetBuffer()
                    .AsSpan(0, (int)_memoryBuffer.Position)
                    .ToArray());
        }

        /// <summary>
        /// In dispose method, we assume that recording has already been stopped.
        /// If memory buffer stream position is not at the start, than, not all data has been pushed to the client yet.
        /// </summary>  
        bool HasStaleData()
        {
            return _memoryBuffer.Position is not 0;
        }
    }

    /// <summary>
    /// Writes audio data to inner memory buffer stream
    /// </summary>
    /// <param name="buffer">Origin data stream</param>
    /// <param name="offset">Offset</param>
    /// <param name="count">Length of actual audio data</param>
    /// <returns>
    /// Audio data, that doesn't fit into the buffer stream in terms of capacity.
    /// This data must be added to the buffer stream after clean up in scope of next 'Write' cycle.
    /// </returns>
    private Span<byte> WriteAudioDataToMemoryBuffer(byte[] buffer, int offset, int count)
    {
        var actualAudioData = buffer.AsSpan(offset, count);
        var availableBufferLength = _memoryBuffer.Capacity - _memoryBuffer.Position;
        var audioDataLengthToBuffer = availableBufferLength > actualAudioData.Length ?
            actualAudioData.Length :
            availableBufferLength;

        var audioDataToBuffer = actualAudioData.Slice(0, (int)audioDataLengthToBuffer);
        _memoryBuffer.Write(audioDataToBuffer);

        return actualAudioData.Length > audioDataLengthToBuffer ?
            actualAudioData.Slice((int)availableBufferLength) :
            default;
    }

    private void ResetMemoryBuffer(Span<byte> freshAudioData)
    {
        _bufferedChunkDurationSec = 0;
        _memoryBuffer.Seek(0, SeekOrigin.Begin);

        if (freshAudioData.Length is not 0)
        {
            _memoryBuffer.Write(freshAudioData);
        }
    }

    private void OnAudioDataRecorded(byte[] audioData, double chunkDurationSec)
    {
        AudioDataRecorded?.Invoke(new RecordedData(
            AudioData: audioData,
            TotalDurationSec: _totalDurationSec));
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;

        AudioDataRecorded = null;

        _memoryBuffer?.Dispose();
        _memoryBuffer = null!;

        base.Dispose(disposing);

        _disposed = true;   
    }
}