using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Core.Audio;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Sequencer.Core.Utils.Streams;

namespace Render.Sequencer.Core.Utils.Helpers;

internal static class SamplesHelper
{
    internal static double GetSecToDipRatio(double duration, double width)
    {
        return duration / width;
    }

    internal static double GetWidthPerSecond(
        double width,
        bool forRecording,
        double secondsOnScreen,
        double durationSec)
    {
        if (width <= 1)
        {
            return 1;
        }

        if (forRecording)
        {
            return width / secondsOnScreen;
        }

        if (durationSec == 0)
        {
            return width / secondsOnScreen;
        }

        return durationSec < secondsOnScreen ? 
            width / durationSec : 
            width / secondsOnScreen;
    }

    internal static SamplesParams GetRecordingSamplesParams(
        double barsPerSecond,
        double buildSampleIntervalMs,
        int dataLength)
    {
        var timerIntervalSec = buildSampleIntervalMs.ToSeconds();
        var barsPerTimerInterval = timerIntervalSec * barsPerSecond;
        var numberOfBars = (int)Math.Ceiling(barsPerTimerInterval);
        var samplesPerBar = dataLength / numberOfBars;

        return new(numberOfBars, samplesPerBar);
    }

    internal static SamplesParams GetPlayingSamplesParams(
        double barsPerSecond,
        double duration,
        int dataLength)
    {
        var numberOfBars = barsPerSecond * duration;
        var samplesPerBar = dataLength / numberOfBars;

        return new((int)numberOfBars, (int)samplesPerBar);
    }

    internal static Task<AudioSamples> BuildPlayerSamplesAsync(AudioModel audio, BuildSamplesParams buildParams)
    {
        if (audio.HasData)
        {
            return Task.Run(() => BuildInMemoryPlayerSamples(audio, buildParams));
        }
        else if (audio.IsFileExists)
        {
            const double syncSampleBuildingLimitSec = 600;

            return buildParams.Duration > syncSampleBuildingLimitSec ?
                BuildPlayerSamplesInParallelAsync(audio, buildParams) :
                Task.Run(() => BuildStreamPlayerSamples(audio, buildParams));
        }

        return Task.FromResult(AudioSamples.CreateEmpty());
    }

    internal static AudioSamples BuildRecorderSamples(AudioModel audio, BuildSamplesParams buildParams)
    {
        if (audio.HasData)
        {
            return BuildInMemoryRecorderSamples(audio, buildParams);
        }
        else if (audio.IsFileExists)
        {
            return BuildStreamRecorderSamples(audio, buildParams);
        }

        return AudioSamples.CreateEmpty();
    }

    internal static AudioSamples BuildInMemoryRecorderSamples(
        byte[] dataChunk,
        BuildSamplesParams buildParams,
        float[] previousTotalSamples)
    {
        var audioDataChunk = dataChunk.ConvertBitToFloat(buildParams.BitPerSample);
        if (audioDataChunk.Length is 0)
        {
            return AudioSamples.CreateEmpty();
        }

        var samplesParams = GetRecordingSamplesParams(
            barsPerSecond: buildParams.BarsPerSecond,
            buildSampleIntervalMs: buildParams.BuildSampleIntervalMs,
            dataLength: audioDataChunk.Length);

        var barArray = CreateBarArray(
            numberOfBars: samplesParams.NumberOfBars,
            samplesPerBar: samplesParams.SamplesPerBar,
            audioData: audioDataChunk);

        return ConcatRecordingSamples(
            visibleBarsLength: (int)buildParams.VisibleBarsLength,
            totalSamples: previousTotalSamples,
            newSamples: barArray);
    }

    private static AudioSamples BuildStreamRecorderSamples(AudioModel audio, BuildSamplesParams buildParams)
    {
        var audioSamples = BuildStreamPlayerSamples(audio, buildParams);

        return ConcatRecordingSamples(
            visibleBarsLength: (int)buildParams.VisibleBarsLength,
            totalSamples: new float[0],
            newSamples: audioSamples.TotalSamples);
    }

    private static AudioSamples BuildInMemoryRecorderSamples(AudioModel audio, BuildSamplesParams buildParams)
    {
        var audioSamples = BuildInMemoryPlayerSamples(audio, buildParams);

        return ConcatRecordingSamples(
            visibleBarsLength: (int)buildParams.VisibleBarsLength,
            totalSamples: new float[0],
            newSamples: audioSamples.TotalSamples);
    }

    private static AudioSamples BuildStreamPlayerSamples(AudioModel audio, BuildSamplesParams buildParams)
    {
        const int dataBufferSize = 5000000;

        var path = audio.Path;

        var audioBuffer = new byte[dataBufferSize];
        var pcmAudioBuffer = new float[dataBufferSize / 2];
        var samples = new float[(int)(buildParams.BarsPerSecond * buildParams.Duration)];

        var audioBufferSpan = new Span<byte>(audioBuffer);
        var pcmAudioBufferSpan = new Span<float>(pcmAudioBuffer);
        var samplesSpan = new Span<float>(samples);

        var wavStreamParameters = audio is EditableAudioModel editable ? 
            new WavStreamParams(editable.StartTime, editable.EndTime, editable.TotalDuration, buildParams.BitPerSample) : 
            new WavStreamParams();

        using var wavStream = new ReadOnlyWavStream(path!, wavStreamParameters);
        
        var dataLength = (double)wavStream.Length;
        
        var lastSamplePosition = -1;
        var audioReadCount = 0;
        while ((audioReadCount = wavStream.Read(audioBufferSpan)) > 0)
        {
            var durationPerBytesCount = (audioReadCount / dataLength) * buildParams.Duration;
            var audioDataLength = audioBufferSpan
                .Slice(0, audioReadCount)
                .ConvertBitToFloat(buildParams.BitPerSample, pcmAudioBufferSpan);

            var samplesParams = GetPlayingSamplesParams(buildParams.BarsPerSecond, durationPerBytesCount, audioDataLength);
            WriteSamples(
                samples: samplesSpan.Slice(++lastSamplePosition),
                numberOfBars: samplesParams.NumberOfBars,
                samplesPerBar: samplesParams.SamplesPerBar,
                audioData: pcmAudioBuffer);

            lastSamplePosition += samplesParams.NumberOfBars - 1;
        }

        return AudioSamples.CreateTotal(samples);
    }

    private static AudioSamples BuildInMemoryPlayerSamples(AudioModel audio, BuildSamplesParams buildParams)
    {
        var data = audio.Data;
        var audioData = data!.ConvertBitToFloat(buildParams.BitPerSample);
        if (audioData.Length is 0)
        {
            return AudioSamples.CreateEmpty();
        }

        var samplesParams = GetPlayingSamplesParams(buildParams.BarsPerSecond, buildParams.Duration, audioData.Length);
        var barArray = CreateBarArray(
            samplesParams.NumberOfBars,
            samplesParams.SamplesPerBar,
            audioData);

        return AudioSamples.CreateTotal(barArray);
    }

    private static async Task<AudioSamples> BuildPlayerSamplesInParallelAsync(AudioModel audio, BuildSamplesParams buildParams)
    {
        const int parallelTasksCount = 3;

        var startPosition = 0d;
        var splitDurationRatio = 1;
        var splitDuration = buildParams.Duration / parallelTasksCount;
        var sampleBuildingTasks = new Task<AudioSamples>[parallelTasksCount];

        for (int i = 0; i < parallelTasksCount; i++)
        {
            sampleBuildingTasks[i] = CreateSplitSampleBuildingTask(
                audio: audio,
                buildParams: buildParams,
                startPosisiionSec: startPosition,
                endPositionSec: i + 1 == parallelTasksCount ? buildParams.Duration : splitDurationRatio * splitDuration);

            splitDurationRatio++;
            startPosition += splitDuration;
        }

        var results = await Task.WhenAll(sampleBuildingTasks);
        var totalSamples = results.SelectMany(samples => samples.TotalSamples).ToArray();

        return AudioSamples.CreateTotal(totalSamples);

        Task<AudioSamples> CreateSplitSampleBuildingTask(AudioModel audio, BuildSamplesParams buildParams, double startPosisiionSec, double endPositionSec)
        {
            return Task.Run(() =>
            {
                var splitAudioModel = EditableAudioModel.Create(audio.Path!, audio.Key);
                splitAudioModel.StartTime = startPosisiionSec;
                splitAudioModel.EndTime = endPositionSec;
                splitAudioModel.TotalDuration = buildParams.Duration;

                var splitBuildParams = new BuildSamplesParams(
                    bitPerSamples: buildParams.BitPerSample,
                    duration: endPositionSec - startPosisiionSec,
                    barsPerSecond: buildParams.BarsPerSecond,
                    maxSecondsOnScreen: buildParams.MaxSecondsOnScreen,
                    buildSampleIntervalMs: buildParams.BuildSampleIntervalMs,
                    visibleBarsLength: buildParams.VisibleBarsLength);

                return BuildStreamPlayerSamples(splitAudioModel, splitBuildParams);
            });
        }
    }

    /// <summary>
    /// Fill bar array that represents the max amplitude values
    /// for a sample data that each are a 1/6 second in length
    /// </summary>
    private static void WriteSamples(
        Span<float> samples,
        int numberOfBars,
        int samplesPerBar,
        float[] audioData)
    {
        for (int i = 0; i < numberOfBars; i++)
        {
            var value = 0f;
            var barStart = i * samplesPerBar;

            for (int j = 0; j < samplesPerBar; j++)
            {
                value += Math.Abs(audioData[barStart + j]);
            }

            var amplitude = value / samplesPerBar;
            samples[i] = amplitude;
        }
    }

    /// <summary>
    /// Creates bar array that represents the max amplitude values
    /// for a sample data that each are a 1/6 second in length
    /// </summary>
    private static float[] CreateBarArray(
        int numberOfBars,
        int samplesPerBar,
        float[] audioData)
    {
        var barArray = new float[Math.Abs(numberOfBars)];

        for (int i = 0; i < numberOfBars; i++)
        {
            var value = 0.0;
            var barStart = i * samplesPerBar;

            for (int j = 0; j < samplesPerBar; j++)
            {
                value += Math.Abs(audioData[barStart + j]);
            }

            var amplitude = value / samplesPerBar;
            barArray[i] = (float)amplitude;
        }

        return barArray;
    }

    private static AudioSamples ConcatRecordingSamples(
        int visibleBarsLength,
        float[] totalSamples,
        float[] newSamples)
    {
        var newTotalSamples = totalSamples.Concat(newSamples);

        return new (
            lastSamples: newTotalSamples.TakeLast(visibleBarsLength).ToArray(), 
            totalSamples: newTotalSamples.ToArray());
    }
}