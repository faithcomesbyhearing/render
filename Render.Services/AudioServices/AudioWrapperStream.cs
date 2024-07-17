namespace Render.Services.AudioServices
{
    /// <summary>
    /// Wrapper class to avoid memory leak for System.IO.NetFxToWinRtStreamAdapter
    /// in Windows.Media.Playback.MediaPlayer in Plugin.SimpleAudioPlayer UWP implementation
    /// which is retain reference to the audio MemoryStream even after disposing.
    /// See details: https://dev.azure.com/FCBH/Software%20Development/_workitems/edit/7602
    /// </summary>
    internal class AudioWrapperStream : Stream
    {
        private Stream _stream;

        public bool LockDispose { get; set; }

        public override bool CanRead
        {
            get => _stream.CanRead;
        }

        public override bool CanSeek
        {
            get => _stream.CanSeek;
        }

        public override bool CanWrite
        {
            get => _stream.CanWrite;
        }

        public override long Length
        {
            get => _stream.Length;
        }

        public override long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }
        
        public AudioWrapperStream()
        {
            _stream = new MemoryStream();
        }
        
        public AudioWrapperStream(byte[] data)
        {
            _stream = new MemoryStream(data);
        }

        public AudioWrapperStream(Stream stream)
        {
            _stream = stream;
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        public byte[] ToArray()
        {
            if(_stream is MemoryStream ms)
            {
                return ms.ToArray();
            }

            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (LockDispose)
            {
                return;
            }
            
            _stream?.Dispose();
            _stream = null;

            base.Dispose(disposing);
        }   
    }
}