namespace Render.Mocks.AudioRecorder.AudioToneGenerator
{
    public class AudioDataChunk
    {
        public uint ChunkSize { get; set; }

        public short[] WaveData { get; private set; }

        public byte[] WaveDataBytes { get; private set; }

        public AudioDataChunk()
        {
            ChunkSize = 0;
            WaveData = Array.Empty<short>();
        }

        public void AddSampleData(short[] leftChannelBuffer, short[] rightChannelBuffer)
        {
            WaveData = new short[leftChannelBuffer.Length + rightChannelBuffer.Length];
            int bufferOffset = 0;

            for (int index = 0; index < WaveData.Length; index += 2)
            {
                WaveData[index] = leftChannelBuffer[bufferOffset];
                WaveData[index + 1] = rightChannelBuffer[bufferOffset];
                bufferOffset++;
            }

            ChunkSize = (uint)WaveData.Length * 2;
            WaveDataBytes = WaveData.SelectMany(BitConverter.GetBytes).ToArray();
        }
    }
}