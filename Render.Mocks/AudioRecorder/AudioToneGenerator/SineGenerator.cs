namespace Render.Mocks.AudioRecorder.AudioToneGenerator
{
    public class SineGenerator
    {
        public static short[] GenerateSine(double frequency, uint sampleRate, int milliSeconds)
        {
            var secondsInLength = milliSeconds / 1000d;
            var bufferSize = (ushort)(sampleRate * secondsInLength);
            var dataBuffer = new short[bufferSize];

            const int amplitude = 32760;
            double timePeriod = Math.PI * 2 * frequency / sampleRate;

            for (uint index = 0; index < bufferSize - 1; index++)
            {
                dataBuffer[index] = Convert.ToInt16(amplitude * Math.Sin(timePeriod * index));
            }

            return dataBuffer;
        }
    }
}