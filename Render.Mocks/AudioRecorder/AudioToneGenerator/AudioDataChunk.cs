namespace Render.Mocks.AudioRecorder.AudioToneGenerator
{
    public class AudioDataChunk
    {

        public short[] WaveData { get; private set; }

        public byte[] WaveDataBytes { get; private set; }

        public AudioDataChunk()
        {
            WaveData = Array.Empty<short>();
        }

        public void SetSampleData(short[] micBuffer)
        {
            WaveData = micBuffer;
            WaveDataBytes = WaveData.SelectMany(BitConverter.GetBytes).ToArray();
        }
    }
}