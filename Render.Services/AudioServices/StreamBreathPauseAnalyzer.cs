using Render.Models.Sections;
using Render.Services.AudioPlugins.AudioPlayer;

namespace Render.Services.AudioServices
{
    public class StreamBreathPauseAnalyzer : BreathPauseAnalyzer, IBreathPauseAnalyzer
    {
        private ITempAudioService _tempAudioService;
        private int _prevMaxVal;
        private int _prevMinVal;
        
        public bool IsAudioLoaded
        {
            get => SimpleAudioPlayer.TotalDuration > 0;
        }

        public StreamBreathPauseAnalyzer(IAudioPlayer simpleAudioPlayer) : base(simpleAudioPlayer)
        {

        }

        public void LoadAudioAndFindBreathPauses(ITempAudioService tempAudioService)
        {
            _tempAudioService = tempAudioService;

            var loaded = SimpleAudioPlayer.Load(_tempAudioService.OpenAudioStream());
            if (loaded)
            {
                Thread.Sleep(500);

                TotalMilliseconds = (int)(SimpleAudioPlayer.Duration * 1000);

                //Reset all our internal stuff
                Division = new List<int>();
                CurrDivision = 0;
                _prevMaxVal = 0;
                _prevMinVal = 0;
                BreathPauseSegments = new List<TimeMarkers>();

                FindPause();

                AddDivision(TotalMilliseconds);
            }
            else
            {
                throw new Exception("Audio did not load");
            }

        }
        
        public void RemoveLastExtraDivision()
        {
            Division.Remove(TotalMilliseconds);
		}

        protected override void CalculateAudioSampleMaximumValues()
        {
            var maxVals = new List<int>();
            var samplesPerSec = 48000;
            var samplesPerMillisecond = samplesPerSec / 1000;
            var sampleChunk = samplesPerMillisecond * 5; // 5 = milliseconds per chunk

            // sampleChunk is specified for short[] wav data array and wavBufferSize should have x2 size
            var wavBufferSize = sampleChunk * 2;
            byte[] wavBuffer = new byte[wavBufferSize];
            int bytesRead;

            using (var stream = _tempAudioService.OpenAudioStream())
            {
                stream.SkipWavHeader();
                
                while ((bytesRead = stream.Read(wavBuffer, 0, wavBuffer.Length)) > 0)
                {
                    var bytes = bytesRead != wavBuffer.Length
                        ? wavBuffer.Take(bytesRead).ToArray()
                        : wavBuffer;

                    var shorts = BytesToShorts(bytes);
                    var maxVal = GetMaxVal(shorts);
                    maxVals.Add(maxVal);
                }
            }

            _maxVals = maxVals.ToArray();

            double movingAverage = 0;
            for (var maxValIndex = 0; maxValIndex < _maxVals.Length - 1; maxValIndex++)
            {
                movingAverage = (_maxVals[maxValIndex] + 6.0 * movingAverage) / 7.0;
                // amazing non voice detection  movingAverage = Math.Abs( _movingAvg1[maxValIndex] - _movingAvg2[maxValIndex]);
                _maxVals[maxValIndex] = (int)movingAverage;
            }
        }

        private int GetMaxVal(IEnumerable<short> oneSampleWavData)
        {
            var maxVal = 0;
            var minVal = 0;

            foreach (short wavValue in oneSampleWavData)
            {
                if (wavValue > 0)
                {
                    maxVal += wavValue;
                }
                else
                {
                    minVal += wavValue;
                }
            }

            if ((maxVal < 8) || (minVal > -8))
            {
                maxVal = minVal = 0;
            }
            else
            {
                if (maxVal > -minVal)
                {
                    minVal = -maxVal;
                }
                else
                {
                    maxVal = -minVal;
                }

                maxVal = (maxVal - _prevMaxVal);
                minVal = (minVal - _prevMinVal);
                maxVal = _prevMaxVal + (maxVal >> 1) - (maxVal >> 3) + (maxVal >> 4);
                minVal = _prevMinVal + (minVal >> 1) - (minVal >> 3) + (minVal >> 4);
            }

            _prevMaxVal = maxVal;
            _prevMinVal = minVal;

            return maxVal;
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
    }
}