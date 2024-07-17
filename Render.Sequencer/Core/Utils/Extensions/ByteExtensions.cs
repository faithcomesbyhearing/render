namespace Render.Sequencer.Core.Utils.Extensions;

public static class ByteExtensions
{
    public static int ConvertBitToFloat(this Span<byte> input, int bitPerSample, Span<float> output)
    {
        return bitPerSample switch
        {
            16 => input.Convert16BitToFloat(output),
            24 => input.Convert24BitToFloat(output),
            _ => throw new Exception("Invalid bit rate"),
        };
        ;
    }
    
    public static float[] ConvertBitToFloat(this byte[] input, int bitPerSample)
    {
        return bitPerSample switch
        {
            16 => input.Convert16BitToFloat(),
            24 => input.Convert24BitToFloat(),
            _ => throw new Exception("Invalid bit rate"),
        };
    }

    private static int Convert16BitToFloat(this Span<byte> input, Span<float> output)
    {
        int inputSamples = input.Length / 2; // 16 bit input, so 2 bytes per sample
        for (int n = 0; n < inputSamples; n++)
        {
            short sample = BitConverter.ToInt16(input.Slice(n * 2));
            output[n] = sample / 32768f;
        }

        return inputSamples;
    }

    private static int Convert24BitToFloat(this Span<byte> input, Span<float> output)
    {
        int inputSamples = input.Length / 3; // 24 bit input
        for (int n = 0; n < inputSamples; n++)
        {
            int sample = BitConverter.ToInt32(input.Slice(n * 3, 3));
            output[n] = sample / 16777216f;
        }

        return inputSamples;
    }

    private static float[] Convert16BitToFloat(this byte[] input)
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

    private static float[] Convert24BitToFloat(this byte[] input)
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