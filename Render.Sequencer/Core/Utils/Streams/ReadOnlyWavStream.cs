using Render.Sequencer.Core.Utils.Helpers;

namespace Render.Sequencer.Core.Utils.Streams;

/// <summary>
/// Read-only stream to iterate through wav file (excluding header) in specified time range
/// </summary>
internal class ReadOnlyWavStream : Stream
{
    private FileStream _fileStream;
    private long _startByte;
    private long _endByte;
    private long _currentPosition;

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _endByte - _startByte;

    public override long Position
    {
        get => _currentPosition - _startByte;
        set => Seek(value, SeekOrigin.Begin);
    }

    public ReadOnlyWavStream(string path, WavStreamParams parameters)
    {
        _fileStream = new FileStream(path, FileMode.Open, FileAccess.Read); 
        
        SetLimit(_fileStream, parameters);

        _currentPosition = _startByte;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_currentPosition >= _endByte)
        {
            return 0;
        }

        _fileStream.Seek(_currentPosition, SeekOrigin.Begin);
        int bytesRead = _fileStream.Read(buffer, offset, (int)Math.Min(count, _endByte - _currentPosition));
        _currentPosition += bytesRead;
        return bytesRead;
    }

    public override int Read(Span<byte> buffer)
    {
        if (_currentPosition >= _endByte)
        {
            return 0;
        }

        _fileStream.Seek(_currentPosition, SeekOrigin.Begin);
        int bytesRead = _fileStream.Read(buffer.Slice(0, (int)Math.Min(buffer.Length, _endByte - _currentPosition)));
        _currentPosition += bytesRead;
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                _currentPosition = Math.Min(Math.Max(_startByte + offset, _startByte), _endByte);
                break;
            case SeekOrigin.Current:
                _currentPosition = Math.Min(Math.Max(_currentPosition + offset, _startByte), _endByte);
                break;
            case SeekOrigin.End:
                _currentPosition = Math.Min(Math.Max(_endByte + offset, _startByte), _endByte);
                break;
        }

        _fileStream.Seek(_currentPosition, SeekOrigin.Begin);
        return _currentPosition - _startByte;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override void Flush()
    {
        _fileStream.Flush();
    }

    private void SetLimit(FileStream stream, WavStreamParams parameters)
    {
        var headerBuffer = new byte[100];
        var headerBufferSpan = new Span<byte>(headerBuffer);
        stream.Read(headerBufferSpan);

        var headerEndPosition = WavHelper.GetWavHeaderEndPosition(headerBufferSpan);
        var dataLength = stream.Length - headerEndPosition;

        _startByte = headerEndPosition;
        _endByte = stream.Length;

        var startTime = parameters.StartTime;
        var endTime = parameters.EndTime;
        var duration = parameters.TotalDuration;

        if (startTime.HasValue && duration.HasValue)
        {
            var startTimeRatio = startTime / duration;
            _startByte = headerEndPosition + (long)(startTimeRatio * dataLength);
        }

        if (endTime.HasValue && duration.HasValue)
        {
            var endTimeRatio = endTime / duration;
            _endByte = headerEndPosition + (long)(endTimeRatio * dataLength);
        }

        RefineLimit(parameters);
    }

    private void RefineLimit(WavStreamParams parameters)
    {
        if (parameters.BitPerSample is null)
        {
            return;
        }

        var chunkSize = parameters.BitPerSample switch
        {
            16 => 2,
            24 => 3,
            _ => throw new Exception("Invalid bit rate"),
        };

        if (_endByte > _fileStream.Length)
        {
            _endByte = _fileStream.Length;
        }

        if (_startByte % chunkSize > 0)
        {
            _startByte += chunkSize - _startByte % chunkSize;
        }

        if (_endByte % chunkSize > 0)
        {
            _endByte -= chunkSize - _endByte % chunkSize;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fileStream?.Dispose();
        }

        base.Dispose(disposing);
    }
}

internal readonly struct WavStreamParams
{
    public readonly double? StartTime;
    public readonly double? EndTime;
    public readonly double? TotalDuration;
    public readonly int? BitPerSample;

    public WavStreamParams()
    {
    }

    public WavStreamParams(double? startTime, double? endTime, double? totalDuration, int? bitPerSample)
    {
        StartTime = startTime;
        EndTime = endTime;
        TotalDuration = totalDuration;
        BitPerSample = bitPerSample;
    }
}
