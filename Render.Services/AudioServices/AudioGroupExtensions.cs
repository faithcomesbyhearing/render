using Render.Models.Audio;
using Render.Models.Sections;

namespace Render.Services.AudioServices
{
    public static class AudioGroupExtensions
    {
        /// <summary>
        /// Organize audios previously received through the repository into audio groups.
        /// </summary>
        /// <param name="passages"></param>
        /// <returns></returns>
        public static List<AudioGroup> GetAudioGroups(this List<Passage> passages)
        {
            var result = new List<AudioGroup>();

            var drafts = passages.GetDraftAudios();
            var firstRetells = passages.GetBackTranslationRetellAudios();
            var firstSegments = passages.GetBackTranslationSegmentAudios();
            var secondRetells = passages.GetSecondStepBackTranslationRetellAudios();
            var secondSegments = passages.GetSecondStepBackTranslationSegmentAudios();

            if (drafts is not null) result.Add(drafts);
            if (firstRetells is not null) result.Add(firstRetells);
            if (firstSegments is not null) result.Add(firstSegments);
            if (secondRetells is not null) result.Add(secondRetells);
            if (secondSegments is not null) result.Add(secondSegments);

            return result;
        }

        private static AudioGroup GetDraftAudios(this List<Passage> passages)
        {
            var drafts = passages
                .Where(p => p?.HasAudio is true)
                .Select(p => p.CurrentDraftAudio);

            return drafts.Any()
                ? new AudioGroup(AudioStepTypes.Draft, drafts)
                : null;
        }

        private static AudioGroup GetBackTranslationRetellAudios(this IEnumerable<Passage> passages)
        {
            var firstRetells = passages
                .Where(p => p.CurrentDraftAudio?.RetellBackTranslationAudio != null)
                .Select(p => p.CurrentDraftAudio.RetellBackTranslationAudio);

            return firstRetells.Any()
                ? new AudioGroup(AudioStepTypes.BackTranslate, firstRetells)
                : null;
        }

        private static AudioGroup GetBackTranslationSegmentAudios(this IEnumerable<Passage> passages)
        {
            var firstSegments = passages
                .Where(p => p.CurrentDraftAudio?.SegmentBackTranslationAudios?.Any() is true)
                .SelectMany(p => p.CurrentDraftAudio.SegmentBackTranslationAudios);

            return firstSegments.Any()
                ? new AudioGroup(AudioStepTypes.SegmentBackTranslate, firstSegments)
                : null;
        }

        private static AudioGroup GetSecondStepBackTranslationRetellAudios(this IEnumerable<Passage> passages)
        {
            var secondRetells = passages
                .Where(p => p.CurrentDraftAudio?.RetellBackTranslationAudio?.RetellBackTranslationAudio != null)
                .Select(p => p.CurrentDraftAudio.RetellBackTranslationAudio.RetellBackTranslationAudio);

            return secondRetells.Any()
                ? new AudioGroup(AudioStepTypes.BackTranslate2, secondRetells)
                : null;
        }

        private static AudioGroup GetSecondStepBackTranslationSegmentAudios(this IEnumerable<Passage> passages)
        {
            var secondSegments = passages
                .Where(p => p.CurrentDraftAudio?.SegmentBackTranslationAudios?.Any() is true)
                .SelectMany(p => p.CurrentDraftAudio.SegmentBackTranslationAudios)
                .Where(segmentAudio => segmentAudio.RetellBackTranslationAudio != null)
                .Select(segmentAudio => segmentAudio.RetellBackTranslationAudio);

            return secondSegments.Any()
                ? new AudioGroup(AudioStepTypes.SegmentBackTranslate2, secondSegments)
                : null;
        }
    }
}
