using Render.Sequencer.Core.Utils.Helpers;

namespace Render.Sequencer.Core.Audio;

internal static class SequencerAudioExtensions
{
    /// <summary>
    /// Returns ratio for converting audio duration\position in seconds
    /// to view coordinates\size value in DIP.
    /// </summary>
    internal static double GetSecToDipRatio(this SequencerAudio audio, double width)
    {
        return SamplesHelper.GetSecToDipRatio(audio.Duration, width);
    }

    internal static SequencerAudio? FindBy(this IEnumerable<SequencerAudio> audios, double totalPosition)
    {
        if (totalPosition < 0)
        {
            return audios.First();
        }

        if (totalPosition >= audios.Last().EndPosition)
        {
            return audios.Last();
        }

        return audios.FirstOrDefault(audio =>
            totalPosition >= audio.StartPosition &&
            totalPosition < audio.EndPosition);
    }

    internal static bool HasAudioData(this SequencerAudio? audio)
    {
        if (audio is null || audio.Audio is null)
        {
            return false;
        }

        return audio.Audio.IsEmpty is false;
    }

    internal static bool IsEmptyAudios(this SequencerAudio[] audios)
    {
        return audios.Length is 0 || audios.All(audio => audio.HasAudioData() is false);
    }

    internal static bool IsTempAudios(this SequencerAudio[] audios)
    {
        return audios.All(audio => audio.IsTemp);
    }
}