using Newtonsoft.Json;
using Render.Models.Sections;
using Render.TempFromVessel.Kernel;

namespace Render.Models.LocalOnlyData
{
    public class SessionRequirement : ValueObject
    {
        [JsonProperty("PageName")]
        public string PageName { get; private set; }
        
        [JsonProperty("PassageNumber")]
        public PassageNumber PassageNumber { get; private set; }
        
        [JsonProperty("NonDraftTranslationId")]
        public Guid NonDraftTranslationId { get; private set; }
        
        [JsonProperty("RequirementId")]
        public Guid RequirementId { get; private set; }

        public SessionRequirement(string pageName, PassageNumber passageNumber, Guid nonDraftTranslationId, Guid requirementId)
        {
            PageName = pageName;
            PassageNumber = passageNumber;
            NonDraftTranslationId = nonDraftTranslationId;
            RequirementId = requirementId;
        }
    }
}