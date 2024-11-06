using Render.Models.Sections;
using Render.Models.Snapshot;

namespace Render.Services.SnapshotService
{
    public static class SnapshotExtension
    {
        public static IEnumerable<SegmentBackTranslation> GetBackTranslationSegmentAudios(this Snapshot snapshot)
        {
            var hasSegment = snapshot.Passages.All(x => x.CurrentDraftAudio.SegmentBackTranslationAudios != null
                                                     && x.CurrentDraftAudio.SegmentBackTranslationAudios.Count > 0);

            return hasSegment
                ? snapshot.Passages.SelectMany(x => x.CurrentDraftAudio.SegmentBackTranslationAudios)
                : null;
        }

        public static IEnumerable<RetellBackTranslation> GetBackTranslationRetellAudios(this Snapshot snapshot)
        {
            var hasRetell = snapshot.Passages.All(x => x.CurrentDraftAudio?.RetellBackTranslationAudio != null);

            return hasRetell
                ? snapshot.Passages.Select(x => x.CurrentDraftAudio?.RetellBackTranslationAudio)
                : null;
        }

        public static IEnumerable<RetellBackTranslation> GetSecondStepBackTranslationRetellAudios(this Snapshot snapshot)
        {
            var hasRetell = snapshot.CheckSecondStepRetellAudio();

            return hasRetell
                ? snapshot.Passages.Select(x => x.CurrentDraftAudio?.RetellBackTranslationAudio?.RetellBackTranslationAudio)
                : null;
        }

        public static IEnumerable<RetellBackTranslation> GetSecondStepBackTranslationSegmentAudios(this Snapshot snapshot)
        {
            return snapshot.CheckSecondStepSegmentAudio()
                ? snapshot.Passages
                    .SelectMany(x => x.CurrentDraftAudio.SegmentBackTranslationAudios)
                    .Select(segmentAudio => segmentAudio.RetellBackTranslationAudio)
                : null;
        }

        public static bool CheckSegmentAudio(this Snapshot snapshot)
        {
            return snapshot.Passages.All(x => x.CurrentDraftAudio?.SegmentBackTranslationAudios != null
                                           && x.CurrentDraftAudio?.SegmentBackTranslationAudios.Count > 0);
        }

        public static bool CheckSecondStepSegmentAudio(this Snapshot snapshot)
        {
            var hasSegment = snapshot.Passages.All(x => x.CurrentDraftAudio?.SegmentBackTranslationAudios != null
                                                     && x.CurrentDraftAudio?.SegmentBackTranslationAudios.Count > 0);
            if (!hasSegment)
            {
                return false;
            }

            var allSegmentsHaveRetells = snapshot.Passages
                .SelectMany(passage => passage.CurrentDraftAudio.SegmentBackTranslationAudios)
                .Any(segment => segment.RetellBackTranslationAudio == null) is false;

            return allSegmentsHaveRetells;
        }

        public static bool CheckRetellAudio(this Snapshot snapshot)
        {
            return snapshot.Passages.All(x => x.CurrentDraftAudio?.RetellBackTranslationAudio != null);
        }

        public static bool CheckSecondStepRetellAudio(this Snapshot snapshot)
        {
            return snapshot.Passages.All(x => x.CurrentDraftAudio?.RetellBackTranslationAudio?.RetellBackTranslationAudio != null);
        }

        public static List<Snapshot> FindRelatedSnapshots(this List<Snapshot> allSnapshots, Guid lastSnapshotId)
        {
            var relatedSnapshots = new List<Snapshot>();
            FindParent(allSnapshots, lastSnapshotId, relatedSnapshots);
            return relatedSnapshots;
        }

        private static void FindParent(List<Snapshot> snapshots, Guid lastSnapshotId, List<Snapshot> relatedSnapshots)
        {
            var snapshot = snapshots.SingleOrDefault(s => s.Id == lastSnapshotId);
            if (snapshot != null)
            {
                relatedSnapshots.Add(snapshot);
                if (snapshot.ParentSnapshot != Guid.Empty)
                {
                    FindParent(snapshots, snapshot.ParentSnapshot, relatedSnapshots);
                }
            }
        }
    }
}
