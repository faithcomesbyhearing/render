using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Core.Utils.Streams;
using System.Diagnostics;
using System.Text;

namespace Render.Sequencer.Core.Utils.Helpers;

internal static class WavHelper
{
    internal const string DefaultFileName = "Combined_ARS_recording_{0}.wav";
    internal const string RiffHeader = "RIFF";
    internal const string WaveHeader = "WAVE";

    internal static WavMergeResult MergeWavFiles(params string[] sourcePaths)
    {
        if (sourcePaths.Length <= 1)
        {
            return new WavMergeResult(sourcePaths.FirstOrDefault(), sourcePaths);
        }

        const int dataBufferSize = 5000000;
        const int headerBufferSize = 100;

        var wavHeader = new byte[headerBufferSize];
        var wavHeaderSpan = new Span<byte>(wavHeader);
        var wavSources = new List<(string SourcePath, long BodySize)>(sourcePaths.Length);

        //calculate wav content sizes (exclude headers)
        foreach (var sourcePath in sourcePaths)
        {
            using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
            sourceStream.Read(wavHeaderSpan);
            var sourceHeaderEnd = GetWavHeaderEndPosition(wavHeaderSpan);
            wavSources.Add((sourcePath, sourceStream.Length - sourceHeaderEnd));
        }

        var targetDirectory = Path.GetDirectoryName(sourcePaths.Last());
        var targetPath = Path.Combine(targetDirectory!, string.Format(DefaultFileName, Guid.NewGuid()));

        using var targetStream = new FileStream(targetPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);

        WriteWavHeader(targetStream, 1, 16, 48000, wavSources.Sum(wavSource => wavSource.BodySize));
        var buffer = new byte[dataBufferSize];

        foreach (var wavSource in wavSources)
        {
            using var sourceStream = new FileStream(wavSource.SourcePath, FileMode.Open, FileAccess.Read);
            sourceStream.Position = sourceStream.Length - wavSource.BodySize; //skip header

            int bytesRead;
            while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                targetStream.Write(buffer, 0, bytesRead);
            }
        }

        return new WavMergeResult(targetPath, sourcePaths);
    }

    internal static WavMergeResult MergeWavFiles(params EditableAudioModel[] audios)
    {
        if (audios.Length is 0)
        {
            return new WavMergeResult(null, Array.Empty<string>());
        }

        if (IsCompleteAudio(audios))
        {
            return new WavMergeResult(audios[0].Path, Array.Empty<string>());
        }

        const int dataBufferSize = 5000000;

        var sources = new List<(EditableAudioModel Audio, long BodySize)>(audios.Length);

        //calculate wav content sizes (exclude headers)
        foreach (var audio in audios)
        {
            using var wavStream = new ReadOnlyWavStream(audio.Path!, new WavStreamParams(audio.StartTime, audio.EndTime, audio.TotalDuration, 16));
            sources.Add((audio, wavStream.Length));
        }

        var targetDirectory = Path.GetDirectoryName(audios.Last().Path);
        var targetPath = Path.Combine(targetDirectory!, string.Format(DefaultFileName, Guid.NewGuid()));

        using var targetStream = new FileStream(targetPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);

        WriteWavHeader(targetStream, 1, 16, 48000, sources.Sum(wavSource => wavSource.BodySize));
        var buffer = new byte[dataBufferSize];

        foreach (var source in sources)
        {
            var audio = source.Audio;
            using var wavStream = new ReadOnlyWavStream(audio.Path!, new WavStreamParams(audio.StartTime, audio.EndTime, audio.TotalDuration, 16));

            int bytesRead;
            while ((bytesRead = wavStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                targetStream.Write(buffer, 0, bytesRead);
            }
        }

        return new WavMergeResult(targetPath, sources.Select(source => source.Audio.Path!).ToArray());
    }

	private static bool IsCompleteAudio(params EditableAudioModel[] audios)
	{
		if (audios.Length == 0)
		{
			return false;
		}

		var firstChunk = audios.First();
		if (audios.Length == 1)
		{
			return firstChunk.StartTime == 0 && firstChunk.EndTime == firstChunk.TotalDuration;
		}

		//check if created from multiple audios
		if (audios.DistinctBy(audio => audio.Path).Count() > 1)
		{
			return false;
		}

		audios = audios.OrderBy(audio => audio.StartTime).ToArray();

		var lastChunk = audios.Last();

		//check if audio boundaries are complete
		if (firstChunk.StartTime != 0 || lastChunk.EndTime != lastChunk.TotalDuration)
		{
			return false;
		}

		//check if complete list of chunks are represented one by one 
		for (var i = 0; i < audios.Length - 1; i++)
		{
			if (audios[i].EndTime != audios[i + 1].StartTime)
			{
				return false;
			}
		}

		return true;
	}

	internal static void WriteWavHeader(Stream stream, ushort channelCount, ushort bitDepth, int sampleRate, long totalSampleCount)
    {
        stream.Position = 0;

        // RIFF header.
        // Chunk ID.
        stream.Write(Encoding.ASCII.GetBytes(RiffHeader), 0, 4);

        // Chunk size.
        stream.Write(BitConverter.GetBytes(((bitDepth / 8) * totalSampleCount) + 36), 0, 4);

        // Format.
        stream.Write(Encoding.ASCII.GetBytes(WaveHeader), 0, 4);

        // Sub-chunk 1.
        // Sub-chunk 1 ID.
        stream.Write(Encoding.ASCII.GetBytes("fmt "), 0, 4);

        // Sub-chunk 1 size.
        stream.Write(BitConverter.GetBytes(16), 0, 4);

        // Audio format.
        stream.Write(BitConverter.GetBytes((ushort)1), 0, 2);

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

    internal static int GetWavHeaderEndPosition(ReadOnlySpan<byte> data)
    {
        if (HasWavHeader(data.Slice(0, RiffHeader.Length)) is false)
        {
            // Assume it's a valid WAV file without WAV-header.
            return 0;
        }

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
            Debug.WriteLine(e.Message);

            // This should never happens.
            return -1;
        }
    }

    private static bool HasWavHeader(ReadOnlySpan<byte> data)
    {
        if (data.Length < RiffHeader.Length)
        {
            return false;
        }

        return Encoding.ASCII.GetString(data) is RiffHeader;
    }

    internal class WavMergeResult
    {
        public readonly string? MergedFilePath;
        public readonly string[] SourceFilePaths;

        public WavMergeResult(string? mergedFilePath, string[] sourceFilePaths)
        {
            MergedFilePath = mergedFilePath;
            SourceFilePaths = sourceFilePaths;
        }

        public void DeleteSourceFiles()
        {
            foreach (var sourceFilePath in SourceFilePaths)
            {
                if (File.Exists(sourceFilePath))
                {
                    File.Delete(sourceFilePath);
                }
            }
        }
    }
}