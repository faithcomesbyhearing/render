using System.Text;
using Newtonsoft.Json;
using ReactiveUI.Fody.Helpers;
using Render.Models.Scope;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Audio
{
    public class Audio : ScopeDomainEntity, IAggregateRoot
    {
        /// <summary>
        /// The audio data, including the format information. This may be WebM, or it may be WAV or something else.
        /// The caller is expected to interpret it correctly. This data is built when the Audio is instantiated by
        /// the Audio Repository.
        /// </summary>
        [Reactive]
        [JsonIgnore]
        public byte[] Data { get; private set; } = Array.Empty<byte>();

        [JsonIgnore] public bool HasAudio => Data?.Length > 0 && !TemporaryDeleted;

        [JsonIgnore]
        public double Duration { get; set; }

        /// <summary>
        /// Duration in seconds
        /// </summary>
        [JsonProperty("SavedDuration")]
        public double SavedDuration { get; set; }

        [JsonProperty("ParentId")]
        public Guid ParentId { get; set; }

        [JsonProperty("TemporaryDeleted")]
        public bool TemporaryDeleted { get; set; }

        private int _headerEndPosition;

        [JsonIgnore]
        public bool DataIsWav => HasAudio && RemoveWavHeader()?.Length > 0;

        [JsonIgnore]
        public int DeclaredLength {get; private set;}
        
        [JsonIgnore]
        public string DeclaredDigest {get; private set;}
        
        public Audio(Guid scopeId, Guid projectId, Guid parentId, int documentVersion = 4)
            : base(scopeId, projectId, documentVersion)
        {
            ParentId = parentId;
        }

        public void SetAudio(byte[] audio)
        {
            Data = audio;
            _headerEndPosition = FindHeaderEndPosition();
        }
        
        public void SetAudio(byte[] audio, int declaredLength, string declaredDigest)
        {
            SetAudio(audio);
            DeclaredLength = declaredLength;
            DeclaredDigest = declaredDigest;
        }

        private int FindHeaderEndPosition()
        {
            return FindWavHeaderEndPosition(Data);
        }

        public static int FindWavHeaderEndPosition(byte[] data)
        {
            try
            {
                // Get past all the other sub chunks to get to the data SubChunk:
                var pos = 12; // First SubChunk ID from 12 to 16

                // Keep iterating until we find the data chunk (i.e. 64 61 74 61 ...... (i.e. 100 97 116 97 in decimal))
                while (!(data[pos] == 100 && data[pos + 1] == 97 && data[pos + 2] == 116 && data[pos + 3] == 97))
                {
                    pos += 4;
                    var chunkSize = data[pos] + (data[pos + 1] * 256) + (data[pos + 2] * 65536) + (data[pos + 3] * 16777216);
                    pos += 4 + chunkSize;
                }

                pos += 8;

                return pos;
            }
            catch (Exception e)
            {
                return -1;
            }
        }

        public byte[] RemoveWavHeader()
        {
            if (_headerEndPosition < 0)
            {
                _headerEndPosition = FindHeaderEndPosition();
            }

            var dataWithoutHeader = _headerEndPosition < 0
                ? Array.Empty<byte>()
                : new ArraySegment<byte>(Data, _headerEndPosition, Data.Length - _headerEndPosition).ToArray();

            return dataWithoutHeader;
        }

        [Obsolete("Calculates Audio Duration only for WAV audio format!")]
        public void SetDuration()
        {
            if (Duration == 0.0)
            {
                var rawData = RemoveWavHeader();
                Duration = (double)rawData.Length / 96000;
            }
        }

        public void AddRawAudioData(byte[] audioData)
        {
            var temp = Data ?? Array.Empty<byte>();
            var newData = new byte[temp.Length + audioData.Length];
            Buffer.BlockCopy(temp, 0, newData, 0, temp.Length);
            Buffer.BlockCopy(audioData, 0, newData, temp.Length, audioData.Length);
            SetAudio(newData);
        }

        public void WriteWavHeader()
        {
            using (var ms = new MemoryStream())
            {
                WriteWavHeaderOnMemoryStream(ms, false, 1, 16, 48000, Data.Length / 2);
                ms.Write(Data, 0, Data.Length);
                ms.Position = 0;
                var buff = new byte[ms.Length];
                ms.Read(buff, 0, buff.Length);
                Data = buff;
            }
        }

        public static void WriteWavHeaderOnStream(Stream stream, int totalSampleCount)
        {
            WriteWavHeaderOnMemoryStream(stream, false, 1, 16, 48000, totalSampleCount);
        }

        private static void WriteWavHeaderOnMemoryStream(Stream stream, bool isFloatingPoint, ushort channelCount, ushort bitDepth, int sampleRate, int totalSampleCount)
        {
            stream.Position = 0;

            // RIFF header.
            // Chunk ID.
            stream.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);

            // Chunk size.
            stream.Write(BitConverter.GetBytes(((bitDepth / 8) * totalSampleCount) + 36), 0, 4);

            // Format.
            stream.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);

            // Sub-chunk 1.
            // Sub-chunk 1 ID.
            stream.Write(Encoding.ASCII.GetBytes("fmt "), 0, 4);

            // Sub-chunk 1 size.
            stream.Write(BitConverter.GetBytes(16), 0, 4);

            // Audio format (floating point (3) or PCM (1)). Any other format indicates compression.
            stream.Write(BitConverter.GetBytes((ushort)(isFloatingPoint ? 3 : 1)), 0, 2);

            // Channels.
            stream.Write(BitConverter.GetBytes(channelCount), 0, 2);

            // Sample rate.
            stream.Write(BitConverter.GetBytes(sampleRate), 0, 4);

            // Bytes rate.
            stream.Write(BitConverter.GetBytes(sampleRate * channelCount * (bitDepth / 8)), 0, 4);

            // Block align.
            stream.Write(BitConverter.GetBytes((ushort)channelCount * (bitDepth / 8)), 0, 2);

            // Bits per sample.
            stream.Write(BitConverter.GetBytes(bitDepth), 0, 2);

            // Sub-chunk 2.
            // Sub-chunk 2 ID.
            stream.Write(Encoding.ASCII.GetBytes("data"), 0, 4);

            // Sub-chunk 2 size.
            stream.Write(BitConverter.GetBytes((bitDepth / 8) * totalSampleCount), 0, 4);
        }
        
        public float[] FilterAudioData(byte[] data, double numberOfBars = 1, bool useSampleRate = false)
        {
            var bitPerSample = 16; //16, 24, 32 not allow 8 bits, normally 16
            //var sampleRate = 48000;
            float[] audioDataAsFloat;

            switch (bitPerSample)
            {
                case 16:
                    audioDataAsFloat = Convert16BitToFloat(data);
                    break;
                case 24:
                    audioDataAsFloat = Convert24BitToFloat(data);
                    break;
                default:
                    throw new Exception("Invalid bit rate");
            }

            if (audioDataAsFloat != null && audioDataAsFloat.Length > 0)
            {
                //for  waveform player that displays one waveform for example the note placement player
                //var samplesPerBar = (int)(sampleRate / numberOfBarsPerSecond);
                var numberOfBarsAsInt = (int)numberOfBars;
                if (numberOfBars != 0 && numberOfBarsAsInt != 0)
                {
                    var samplesPerBar = audioDataAsFloat.Length / numberOfBarsAsInt;
                    if (samplesPerBar == 0)
                    {
                        samplesPerBar = 1;
                    }
                    if (useSampleRate)
                    {
                        numberOfBarsAsInt = audioDataAsFloat.Length / samplesPerBar;
                    }

                    if (!useSampleRate) //for waveform player that displays waveform in segments like the tablet segment review player
                    {
                        samplesPerBar = (int)numberOfBars >= audioDataAsFloat.Length
                            ? 1
                            : (int)(audioDataAsFloat.Length / numberOfBars);
                    }

                    float[] barArray = new float[Math.Abs(numberOfBarsAsInt)];

                    //create bar array that represents the max amplitude values
                    //for a sample data that each are a 1/6 second in length
                    for (int i = 0; i < numberOfBarsAsInt; i++)
                    {
                        var value = 0.0;
                        var barStart = i * samplesPerBar;
                        for (int j = 0; j < samplesPerBar; j++)
                        {
                            value += Math.Abs(audioDataAsFloat[barStart + j]);
                        }

                        var amplitude = value / samplesPerBar;
                        barArray[i] = (float)amplitude;
                    }

                    return barArray;
                }
            }

            return null;
        }

        private float[] Convert16BitToFloat(byte[] input)
        {
            int inputSamples = input.Length / 2; // 16 bit input, so 2 bytes per sample
            float[] output = new float[inputSamples];
            int outputIndex = 0;
            for (int n = 0; n < inputSamples; n++)
            {
                short sample = BitConverter.ToInt16(input, n * 2);
                output[outputIndex++] = sample / 32768f;
            }

            return output;
        }

        private float[] Convert24BitToFloat(byte[] input)
        {
            int inputSamples = input.Length / 3; // 24 bit input
            float[] output = new float[inputSamples];
            int outputIndex = 0;
            var temp = new byte[4];
            for (int n = 0; n < inputSamples; n++)
            {
                // copy 3 bytes in
                Array.Copy(input, n * 3, temp, 0, 3);
                int sample = BitConverter.ToInt32(temp, 0);
                output[outputIndex++] = sample / 16777216f;
            }

            return output;
        }
    }
}