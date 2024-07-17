using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Snapshot
{
    public class PassageBackTranslation : ValueObject
    {
        [JsonProperty("PassageId")] public Guid PassageId { get; }
        [JsonProperty("RetellBackTranslationId")] public Guid RetellBackTranslationId { get; }
        [JsonProperty("SegmentBackTranslationIds")] public List<Guid> SegmentBackTranslationIds { get; }

        public PassageBackTranslation(Guid passageId, Guid retellBackTranslationId, List<Guid> segmentBackTranslationIds)
        {
            PassageId = passageId;
            RetellBackTranslationId = retellBackTranslationId;
            SegmentBackTranslationIds = segmentBackTranslationIds;
        }
    }
}