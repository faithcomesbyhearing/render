using System.IO;
using Render.Models.Audio;

namespace Render.Services.AudioServices
{
    public static class WavStreamExtensions
    {
        public static int SkipWavHeader(this Stream wavStream)
        {
            // seek stream to skip WAV header
            var wavBufferSize = 1000;
            byte[] wavBuffer = new byte[wavBufferSize];

            wavStream.Read(wavBuffer, 0, wavBuffer.Length);
            var wavHeaderEndPosition = Audio.FindWavHeaderEndPosition(wavBuffer);
            wavStream.Seek(wavHeaderEndPosition, SeekOrigin.Begin);

            return wavHeaderEndPosition;
        }
    }
}