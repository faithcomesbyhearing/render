using Newtonsoft.Json;

namespace Render.Models.Sections
{
    public class SegmentBackTranslation : BackTranslation
    {

        [JsonProperty("TimeMarkers")]
        public TimeMarkers TimeMarkers { get; set; }

        [JsonProperty("CorrespondedSnashotId")]
        public Guid CorrespondedSnashotId { get; set; }

        public SegmentBackTranslation(
            TimeMarkers timeMarkers,
            Guid parentId,
            Guid toLanguageId,
            Guid fromLanguageId,
            Guid projectId,
            Guid scopeId)
            : base(
                  parentId,
                  toLanguageId,
                  fromLanguageId,
                  projectId,
                  scopeId,
                  documentVersion: 3)
        {
            TimeMarkers = timeMarkers;
        }
    }
}