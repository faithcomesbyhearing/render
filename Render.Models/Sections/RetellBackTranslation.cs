using Newtonsoft.Json;

namespace Render.Models.Sections
{
    public class RetellBackTranslation : BackTranslation
    {
        [JsonProperty("CorrespondedSnashotId")]
        public Guid CorrespondedSnashotId { get; set; }

        public RetellBackTranslation(
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
        }
    }
}