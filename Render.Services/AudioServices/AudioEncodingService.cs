using Concentus.Enums;
using Concentus.Oggfile;
using Concentus.Structs;
using Render.Interfaces;
using Render.Models.Audio;

namespace Render.Services.AudioServices
{
    public class AudioEncodingService : IAudioEncodingService
    {
        public const int DefaultWavHeaderSize = 44;

        private readonly IRenderLogger _logger;

        public AudioEncodingService(IRenderLogger logger)
        {
            _logger = logger;
        }

        public Task<byte[]> ConvertWavToOpusAsync(Stream wavStream, int sampleRate, int channelCount)
        {
            return Task.Run(() => ConvertWavToOpus(wavStream, sampleRate, channelCount));
        }

        public byte[] ConvertWavToOpus(Stream wavStream, int sampleRate, int channelCount)
        {
            using (var memoryOut = new MemoryStream())
            {
                var encoder = OpusEncoder.Create(sampleRate, channelCount, OpusApplication.OPUS_APPLICATION_AUDIO);
                encoder.Bitrate = sampleRate;
                var oggOut = new OpusOggWriteStream(encoder, memoryOut, new OpusTags());

                const int wavBufferSize = 5000000; // 5 Megabytes
                byte[] wavBuffer = new byte[wavBufferSize];
                bool wavHeaderRemoved = false;
                int bytesRead;

                while ((bytesRead = wavStream.Read(wavBuffer, 0, wavBuffer.Length)) > 0)
                {
                    var bytes = bytesRead != wavBuffer.Length
                        ? wavBuffer.Take(bytesRead).ToArray()
                        : wavBuffer;

                    if (!wavHeaderRemoved)
                    {
                        var wavHeaderEndPosition = Audio.FindWavHeaderEndPosition(bytes);
                        bytes = bytes.Skip(wavHeaderEndPosition).ToArray();

                        wavHeaderRemoved = true;
                    }

                    var samples = BytesToShorts(bytes);
                    oggOut.WriteSamples(samples, 0, samples.Length);
                }

                oggOut.Finish();
                return memoryOut.ToArray();
            }
        }

        public void ConvertOpusToWav(byte[] opusData, int sampleRate, int channelCount, Stream outWavStream)
        {
            outWavStream.Seek(DefaultWavHeaderSize, SeekOrigin.Begin);

            var totalSamplesCount = ConvertOpusToWav(opusData, sampleRate, channelCount, outWavStream, cancellationToken: default);

            Audio.WriteWavHeaderOnStream(outWavStream, totalSamplesCount);
        }

        private void ConcatenateIntoWav(IEnumerable<byte[]> opusAudios, int sampleRate, int channelCount, Stream outWavStream, CancellationToken cancellationToken)
        {
            outWavStream.Seek(DefaultWavHeaderSize, SeekOrigin.Begin);

            int totalSamplesCount = 0;
            foreach (var opusData in opusAudios)
            {
                totalSamplesCount += ConvertOpusToWav(opusData, sampleRate, channelCount, outWavStream, cancellationToken);
            }

            Audio.WriteWavHeaderOnStream(outWavStream, totalSamplesCount);
        }

        public void ConcatenateIntoWav(IEnumerable<byte[]> opusAudios, int sampleRate, int channelCount, Stream outWavStream)
        {
            ConcatenateIntoWav(opusAudios, sampleRate, channelCount, outWavStream, cancellationToken: default);
        }

        public async Task ConcatenateIntoWavAsync(IEnumerable<byte[]> opusAudios, int sampleRate, int channelCount, Stream outWavStream, CancellationToken cancellationToken)
        {
            await Task.Run(() => { ConcatenateIntoWav(opusAudios, sampleRate, channelCount, outWavStream, cancellationToken); }, cancellationToken);
        }

        public async Task<List<string>> SplitOpus(byte[] opusData, string tempAudioDirectoryPath, int sampleRate, int channelCount,
            List<int> timeMarkersInMilliseconds)
        {
            var result = new List<string>();
            var tempAudioPath = GetTempAudioPath(tempAudioDirectoryPath);
            var fsWav = new FileStream(tempAudioPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            var stream = new AudioWrapperStream(opusData);

            try
            {
                OpusDecoder decoder = OpusDecoder.Create(sampleRate, channelCount);
                OpusOggReadStream oggIn = new OpusOggReadStream(decoder, stream);
                Audio.WriteWavHeaderOnStream(fsWav, (int)oggIn.GranuleCount);

                // Calculate the number of samples per millisecond
                int samplesPerMillisecond = sampleRate / 1000;
                int elapsedTimeInMilliseconds = 0;
                int splitIndex = 0;

                while (oggIn.HasNextPacket)
                {
                    short[] packet = oggIn.DecodeNextPacket();
                    if (packet != null)
                    {
                        byte[] binary = ShortsToBytes(packet);

                        // Update the elapsed time based on the packet length
                        elapsedTimeInMilliseconds += packet.Length / samplesPerMillisecond;

                        // Check if the elapsed time exceeds the current split time
                        if (splitIndex < timeMarkersInMilliseconds.Count &&
                            elapsedTimeInMilliseconds >= timeMarkersInMilliseconds[splitIndex])
                        {
                            await CloseStreamAsync(fsWav);

                            result.Add(tempAudioPath);

                            tempAudioPath = GetTempAudioPath(tempAudioDirectoryPath);
                            fsWav = new FileStream(tempAudioPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                            Audio.WriteWavHeaderOnStream(fsWav, (int)oggIn.GranuleCount);

                            splitIndex++;
                        }

                        await fsWav.WriteAsync(binary, 0, binary.Length);
                    }
                }

                result.Add(tempAudioPath);
                return result;
            }
            finally
            {
                stream.Dispose();
                await CloseStreamAsync(fsWav);
            }
        }

        private string GetTempAudioPath(string tempAudioDirectoryPath)
        {
            return Path.Combine(tempAudioDirectoryPath, $"{Guid.NewGuid()}.wav");
        }

        private async Task CloseStreamAsync(FileStream fsWav)
        {
            await fsWav.FlushAsync();
            fsWav.Close();
            await fsWav.DisposeAsync();
        }

        public async Task<List<string>> SplitOpus(byte[] opusData, string tempAudioDirectoryPath, int sampleRate, int channelCount, int timeMarkerInMilliseconds)
        {
            return await SplitOpus(opusData, tempAudioDirectoryPath, sampleRate, channelCount, new List<int> { timeMarkerInMilliseconds });
        }

        private int ConvertOpusToWav(byte[] opusData, int sampleRate, int channelCount, Stream outWavStream, CancellationToken cancellationToken)
        {
            using var stream = new AudioWrapperStream(opusData);
            OpusDecoder decoder = OpusDecoder.Create(sampleRate, channelCount);
            OpusOggReadStream oggIn = new OpusOggReadStream(decoder, stream);

            var totalSamplesCount = 0;
            while (oggIn.HasNextPacket)
            {
                cancellationToken.ThrowIfCancellationRequested();

                short[] packet = oggIn.DecodeNextPacket();
                if (packet != null)
                {
                    byte[] binary = ShortsToBytes(packet);
                    outWavStream.Write(binary, 0, binary.Length);

                    totalSamplesCount += packet.Length;
                }
            }

            return totalSamplesCount;
        }

        /// <summary>
        /// Converts interleaved byte samples (such as what you get from a capture device)
        /// into linear short samples (that are much easier to work with)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static short[] BytesToShorts(byte[] input)
        {
            return BytesToShorts(input, 0, input.Length);
        }

        /// <summary>
        /// Converts interleaved byte samples (such as what you get from a capture device)
        /// into linear short samples (that are much easier to work with)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static short[] BytesToShorts(byte[] input, int offset, int length)
        {
            short[] processedValues = new short[length / 2];
            for (int c = 0; c < processedValues.Length; c++)
            {
                processedValues[c] = (short)(((int)input[(c * 2) + offset]) << 0);
                processedValues[c] += (short)(((int)input[(c * 2) + 1 + offset]) << 8);
            }

            return processedValues;
        }

        /// <summary>
        /// Converts linear short samples into interleaved byte samples, for writing to a file, waveout device, etc.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static byte[] ShortsToBytes(short[] input)
        {
            return ShortsToBytes(input, 0, input.Length);
        }

        /// <summary>
        /// Converts linear short samples into interleaved byte samples, for writing to a file, waveout device, etc.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static byte[] ShortsToBytes(short[] input, int in_offset, int samples)
        {
            byte[] processedValues = new byte[samples * 2];
            ShortsToBytes(input, in_offset, processedValues, 0, samples);
            return processedValues;
        }

        /// <summary>
        /// Converts linear short samples into interleaved byte samples, for writing to a file, waveout device, etc.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static void ShortsToBytes(short[] input, int in_offset, byte[] output, int out_offset, int samples)
        {
            for (int c = 0; c < samples; c++)
            {
                output[(c * 2) + out_offset] = (byte)(input[c + in_offset] & 0xFF);
                output[(c * 2) + out_offset + 1] = (byte)((input[c + in_offset] >> 8) & 0xFF);
            }
        }
    }
}