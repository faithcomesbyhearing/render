using Render.Models.Audio;
using Render.Models.Sections;
using Render.Services.AudioPlugins.AudioPlayer;

namespace Render.Services.AudioServices
{
    // abstract: use StreamBreathPauseAnalyzer class.
    public abstract class BreathPauseAnalyzer
    {
        public List<int> Division { get; protected set; }

        public List<TimeMarkers> BreathPauseSegments { get; protected set; } = new List<TimeMarkers>();

        /// <summary>
        ///     Array of maximum values of the waveform
        /// </summary>
        protected int[] _maxVals;

        /// <summary>
        ///     This is the WaveFile class variable that describes the internal structures of the .WAV
        /// </summary>
        private WaveFile _wavefile;

        /// <summary>
        ///     Used to define start and end positions (in _maxVals array) where pauses are found in audio
        /// </summary>
        private struct Section
        {
            public int Start;
            public int End;
        };

        protected readonly IAudioPlayer SimpleAudioPlayer;
        protected int CurrDivision;
        protected int TotalMilliseconds;
        private double _strength = 4;

        /// <summary>
        /// Automatically divides an mp3 wavestream and puts the result in the back translate table
        /// </summary>

        protected BreathPauseAnalyzer(IAudioPlayer simpleAudioPlayer)
        {
            SimpleAudioPlayer = simpleAudioPlayer;
        }

        public virtual void LoadAudioAndFindBreathPauses(Audio audio)
        {
            var stream = new MemoryStream(audio.Data);
            using (var wrapperStream = new AudioWrapperStream(stream))
            {
                var loaded = SimpleAudioPlayer.Load(wrapperStream);
                if (loaded)
                {
                    Thread.Sleep(500);
                    TotalMilliseconds = (int)(SimpleAudioPlayer.Duration * 1000);
                    stream.Seek(0, SeekOrigin.Begin);
                    _wavefile = new WaveFile(stream);
                    _wavefile.Read();
                    //Reset all our internal stuff
                    Division = new List<int>();
                    CurrDivision = 0;
                    BreathPauseSegments = new List<TimeMarkers>();
                    FindPause();
                    AddDivision(TotalMilliseconds);
                    stream.Dispose();
                }
                else
                {
                    throw new Exception("Audio did not load");
                }
            }
        }

        /// <summary>
        /// Create a new division
        /// </summary>
        protected void AddDivision(int breathPausePosition)
        {
            if (CurrDivision > 0 && Division[CurrDivision - 1] >= TotalMilliseconds)
                return;
            Division.Add(breathPausePosition);
            if (CurrDivision == 0)
            {
                var timeMarker = new TimeMarkers(0, breathPausePosition);
                BreathPauseSegments.Add(timeMarker);
            }

            if (CurrDivision > 0)
            {
                var timeMarker = new TimeMarkers(Division[CurrDivision - 1], breathPausePosition);
                BreathPauseSegments.Add(timeMarker);
            }

            CurrDivision++;
        }

        /// <summary>
        /// Get the word(s) end position if a word is found starting at the specified start position
        /// </summary>
        /// <param name="startPosition">The position to start looking for a word.</param>
        /// <param name="wordThreshold">The threshold point at which a sound is qualified as a word</param>
        /// <param name="preFetchRange">Indicates how far to look ahead of the start position for a sound that meets the word threshold</param>
        /// <param name="sustainedThresholdRange">Indicates how long a sound must be sustained at a the word threshold to be considered a word</param>
        /// <param name="sustainedPauseRange">
        /// Indicates how long a sound must be sustained below a percent of word threshold to be considered a pause.
        /// This is a very good setting to play with to adjust the number of pauses.  The higher the number, the less pauses
        /// </param>
        /// <param name="pauseThresholdPercent">The highest percentage of the wordThreshold that a sound is still considered a pause</param>
        /// <returns>
        /// The position of where the word end or -1 if no word is found
        /// </returns>
        private int GetWordsEndPosition(int startPosition, double wordThreshold, int preFetchRange,
            int sustainedThresholdRange, int sustainedPauseRange, int pauseThresholdPercent)
        {
            var prefetchEnd = startPosition + preFetchRange;
            var pauseThresholdFactor = pauseThresholdPercent / (double)100;

            //if not enough audio left to detect word then quit;
            if (prefetchEnd >= _maxVals.Length || startPosition + sustainedThresholdRange >= _maxVals.Length)
                return -1;

            var wordFound = false;
            var thresholdCount = 0;
            var thresholdFailureCount = 0;
            var position = startPosition;
            while (position < _maxVals.Length)
            {
                if (_maxVals[position] < (wordThreshold * pauseThresholdFactor))
                {
                    //if word not found in preFetchRange then quit
                    if (!wordFound && position > preFetchRange)
                    {
                        break;
                    }

                    thresholdCount = 0;
                    thresholdFailureCount++;
                    //if sustained quiet quit
                    if (thresholdFailureCount > sustainedPauseRange)
                    {
                        //find next point where audio breaks threshold
                        var checkPos = position + 1;
                        var thresholdFound = false;
                        while (checkPos < _maxVals.Length)
                        {
                            thresholdFound = _maxVals[checkPos] >= wordThreshold;
                            if (thresholdFound) break;
                            checkPos++;
                        }

                        //if threshold broken ahead of this position and nearby, try to backup pause
                        //position to accommodate
                        if (thresholdFound)
                        {
                            var backupOffset = checkPos - position - sustainedPauseRange;
                            //if room, backup to beginning of where pause was detected
                            if (backupOffset >= 0)
                            {
                                position += -sustainedPauseRange;
                                break;
                            }

                            //if not enough room to back up to begin of pause, try to back up position
                            //to position where pause buffer exists before next point of word threshold
                            if (-backupOffset < sustainedPauseRange)
                            {
                                position += backupOffset;
                                break;
                            }

                            //if can't leave a enough buffer before next point threshold is broken 
                            thresholdFailureCount = 0;
                        }
                        else
                        {
                            //since pause must occur over a sustained period, back up exit point
                            //to a position in the beginning of this the sustained pause
                            position += -sustainedPauseRange;
                            break;
                        }
                    }
                }
                else
                {
                    thresholdFailureCount = 0;
                    if (_maxVals[position] >= wordThreshold)
                    {
                        thresholdCount++;
                        if (thresholdCount >= sustainedThresholdRange)
                            wordFound = true;
                    }
                }

                position++;
            }

            return wordFound ? position : -1;
        }

        /// <summary>
        /// Calculates the maximum values for each audio sample.
        /// </summary>
        protected virtual void CalculateAudioSampleMaximumValues()
        {
            var samplesPerMillisecond = _wavefile.Format.SamplesPerSec / 1000;
            var sampleChunk = samplesPerMillisecond * 5; // 5 = milliseconds per chunk
            var prevMaxVal = 0;
            var prevMinVal = 0;
            var sampleChunkIndex = 0;
            var maxValCount = 0;
            _maxVals = new int[_wavefile.Data.NumSamples / sampleChunk];

            while (sampleChunkIndex < _wavefile.Data.NumSamples - sampleChunk)
            {
                var maxVal = 0;
                var minVal = 0;
                for (var dataIndex = sampleChunkIndex; dataIndex < sampleChunkIndex + sampleChunk; dataIndex++)
                {
                    if (_wavefile.Data[dataIndex] > 0)
                    {
                        maxVal += _wavefile.Data[dataIndex];
                    }
                    else
                    {
                        minVal += _wavefile.Data[dataIndex];
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

                    maxVal = (maxVal - prevMaxVal);
                    minVal = (minVal - prevMinVal);
                    maxVal = prevMaxVal + (maxVal >> 1) - (maxVal >> 3) + (maxVal >> 4);
                    minVal = prevMinVal + (minVal >> 1) - (minVal >> 3) + (minVal >> 4);
                }

                prevMaxVal = maxVal;
                prevMinVal = minVal;

                _maxVals[maxValCount] = maxVal;

                maxValCount++;
                sampleChunkIndex += sampleChunk;
            }

            double movingAverage = 0;
            for (var maxValIndex = 0; maxValIndex < maxValCount - 1; maxValIndex++)
            {
                movingAverage = (_maxVals[maxValIndex] + 6.0 * movingAverage) / 7.0;
                // amazing non voice detection  movingAverage = Math.Abs( _movingAvg1[maxValIndex] - _movingAvg2[maxValIndex]);
                _maxVals[maxValIndex] = (int)movingAverage;
            }
        }

        /// <summary>
        /// Calculates the audio average.
        /// </summary>
        /// <param name="previousAverage">
        /// The previous average from the previous average from the last pass (i.e. last time this routine was called).
        /// This parameter is optional. 
        /// </param>
        /// <returns></returns>
        private long CalculateAudioAverage(long previousAverage = 0)
        {
            var averageCount = 0;
            long average = 0;

            foreach (int val in _maxVals)
            {
                if ((val < previousAverage || previousAverage == 0) && (val > 350))
                {
                    averageCount++;
                    average = ((averageCount - 1) * average + val) / averageCount;
                }
            }

            return average;
        }

        protected void FindPause()
        {
            CalculateAudioSampleMaximumValues();

            var firstPassAverage = CalculateAudioAverage();
            var secondPassAverage = CalculateAudioAverage(firstPassAverage);
            var thirdPassAverage = CalculateAudioAverage(secondPassAverage);

            var finalAverage = (secondPassAverage + thirdPassAverage) / 2;

            var wordThreshold = finalAverage * _strength;
            var sectionStart = 0;
            var sectionCount = 0;
            var sectionEnd = 0;
            var isFirstSection = true;
            var pauseSections = new List<Section>();
            for (var position = 1; position < _maxVals.Length; position++)
            {
                //search for words starting at position
                var wordEnd = GetWordsEndPosition(position, wordThreshold, 30, 25, 30, 40);

                if (wordEnd > 0)
                {
                    position = wordEnd + 1;

                    if (isFirstSection)
                    {
                        isFirstSection = false;
                    }
                    else
                    {
                        pauseSections.Add(new Section { Start = sectionStart, End = sectionEnd });
                        sectionCount++;
                    }

                    sectionStart = position;
                }
                else
                {
                    //if start not set, set start to first position that pause (no words) is found
                    if (sectionStart == 0)
                    {
                        sectionStart = position;
                    }

                    //set section end position to last known position where pause (no words) is found
                    sectionEnd = position;
                }
            }

            if (sectionCount < 1)
            {
                //AddDivision(0, _totalMilliseconds);
            }
            else
            {
                var divisionStart = 0;
                for (var sectionIndex = 0; sectionIndex < sectionCount; sectionIndex++)
                {
                    var sectionAudioStart = (int)((pauseSections[sectionIndex].Start / (double)_maxVals.Length) *
                                                  TotalMilliseconds);
                    var sectionAudioEnd = (int)((pauseSections[sectionIndex].End / (double)_maxVals.Length) *
                                                TotalMilliseconds);

                    //calculate end of division as middle of pause
                    var divisionEnd = (sectionAudioEnd + sectionAudioStart) / 2;

                    //if first pause section does not start at the beginning, add a division to
                    //pad up to that point
                    if (sectionIndex == 0 && pauseSections[sectionIndex].Start != 0)
                    {
                        AddDivision(divisionEnd);
                    }
                    else
                    {
                        AddDivision(divisionEnd);
                    }


                    divisionStart = divisionEnd;
                }

                //if last division did not end at the end of the audio, add a division that pads out
                //the end
                if (divisionStart != TotalMilliseconds)
                {
                    //AddDivision(divisionStart, _totalMilliseconds);
                }
            }
        }
    }
}