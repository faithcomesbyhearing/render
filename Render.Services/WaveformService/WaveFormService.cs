﻿using Render.Interfaces;
using Render.Services.AudioServices;

namespace Render.Services.WaveformService;

public class WaveFormService : IWaveFormService
{
    private readonly IRenderLogger _logger;

    private const int MiniWaveFormBarsCount = 100;

    public WaveFormService(IRenderLogger logger)
    {
        _logger = logger;
    }

    public float[] GetEmptyMiniWaveformBars()
    {
        return new float[MiniWaveFormBarsCount];
    }

    public float[] GetMiniWaveformBars(Stream wavStream)
    {
        if (wavStream.Length == 0)
        {
            return Array.Empty<float>();
        }

        var headerLength = wavStream.SkipWavHeader();
        var bodyLength = wavStream.Length - headerLength;

        long totalSamples = bodyLength / 2; // Assuming 16-bit audio
        long samplesPerBar = totalSamples / MiniWaveFormBarsCount;
        byte[] sampleBuffer = new byte[samplesPerBar * 2]; // 2 bytes per sample
        float[] bars = new float[MiniWaveFormBarsCount];

        for (int i = 0; i < MiniWaveFormBarsCount; i++)
        {
            int bytesRead = wavStream.Read(sampleBuffer, 0, sampleBuffer.Length);
            int samplesRead = bytesRead / 2;
            double amplitude = 0;

            try
            {
                for (int j = 0; j < samplesRead; j++)
                {
                    short sample = BitConverter.ToInt16(sampleBuffer, j * 2);
                    amplitude += sample == short.MinValue ? short.MaxValue : Math.Abs(sample);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                continue;
            }

            bars[i] = (float)(amplitude / samplesRead) / short.MaxValue; // Normalize and store average amplitude
        }

        return bars;
    }

    public Task<float[]> GetMiniWaveformBarsAsync(Stream wavStream)
    {
        return Task.Run(() => GetMiniWaveformBars(wavStream));
    }

    public float[] CreateBars(float[] audioData, int numberOfBars, int samplesPerBar)
    {
        float[] barArray = new float[numberOfBars];

        //create bar array that represents the max amplitude values
        //for a sample data that each are a 1/6 second in length
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

    /// <summary>
    /// Upsampling\Downsampling existings bars to specified count of bars.
    /// </summary>
    public float[] InterpolateBars(float[] bars, int requiredCount = MiniWaveFormBarsCount)
    {
        // Check if the required count is valid
        if (requiredCount < 1)
        {
            throw new ArgumentException("Required count must be greater than zero.");
        }

        if(bars.Length == requiredCount || bars.Length == 0)
        {
            return bars;
        }

        int originalCount = bars.Length;
        float[] newBars = new float[requiredCount];
        float scaleFactor = (float)(originalCount) / requiredCount;

        if (requiredCount < originalCount)
        {
            // Downsampling
            for (int i = 0; i < requiredCount; i++)
            {
                float start = i * scaleFactor;
                float end = (i + 1) * scaleFactor;

                int startIndex = (int)Math.Floor(start);
                int endIndex = (int)Math.Min(Math.Ceiling(end), originalCount - 1);

                float sum = 0;
                int count = 0;

                for (int j = startIndex; j <= endIndex; j++)
                {
                    sum += bars[j];
                    count++;
                }

                newBars[i] = sum / count;
            }
        }
        else
        {
            // Upsampling
            for (int i = 0; i < requiredCount; i++)
            {
                float position = i * scaleFactor;
                int leftIndex = (int)Math.Floor(position);
                int rightIndex = Math.Min((int)Math.Ceiling(position), originalCount - 1);

                if (leftIndex == rightIndex)
                {
                    // Exact match or at the boundaries
                    newBars[i] = bars[leftIndex];
                }
                else
                {
                    // Linear interpolation
                    float leftValue = bars[leftIndex];
                    float rightValue = bars[rightIndex];
                    float t = position - leftIndex;
                    newBars[i] = leftValue * (1 - t) + rightValue * t;
                }
            }
        }

        return newBars;
    }

    public float[] ConvertAudioDataToFloat(byte[] audioData, int bitPerSample)
    {
        try
        {
            float[] audioDataAsFloat;

            switch (bitPerSample)
            {
                case 16:
                    audioDataAsFloat = Convert16BitToFloat(audioData);
                    break;
                case 24:
                    audioDataAsFloat = Convert24BitToFloat(audioData);
                    break;
                default:
                    throw new Exception("Invalid bit rate");
            }

            return audioDataAsFloat;
        }
        catch (Exception e)
        {
            _logger.LogError(e);
        }

        return new float[] { };
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