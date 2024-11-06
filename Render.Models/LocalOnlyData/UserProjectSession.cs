using Newtonsoft.Json;
using Render.Models.Sections;
using Render.TempFromVessel.Kernel;

namespace Render.Models.LocalOnlyData
{
    public class UserProjectSession: DomainEntity, IAggregateRoot
    {
        [JsonProperty("UserId")]
        public Guid UserId { get; }
        
        [JsonProperty("ProjectId")]
        public Guid ProjectId { get; }

        [JsonProperty("StepId")]
        public Guid StepId { get; }
        
        [JsonProperty("PageName")]
        public string PageName { get; private set; }

        [JsonProperty("SectionId")]
        public Guid SectionId { get; private set; }
        
        [JsonProperty("PassageNumber")]
        public PassageNumber PassageNumber { get; private set; }
        
        [JsonProperty("NonDraftTranslationId")]
        public Guid NonDraftTranslationId { get; private set; }
        
        [JsonProperty("DraftAudioIds")]
        public List<Guid> DraftAudioIds { get; } = new List<Guid>();
        
        [JsonProperty("CurrentDraftAudioId")]
        public Guid CurrentDraftAudioId { get; set; }
        
        [JsonProperty("RequirementsCompleted")]
        public List<SessionRequirement> RequirementsCompleted { get; } = new List<SessionRequirement>();
        
        [JsonProperty("PreviousDraftId")]
        public Guid PreviousDraftId { get; set; } 

        public UserProjectSession(Guid userId, Guid projectId, Guid stepId)
        : base(Version)
        {
            StepId = stepId;
            UserId = userId;
            ProjectId = projectId;
        }

        public void Reset()
        {
            SectionId = Guid.Empty;
            PageName = "";
            PassageNumber = null;
            DraftAudioIds.Clear();
            CurrentDraftAudioId = Guid.Empty;
            PreviousDraftId = Guid.Empty;
            RequirementsCompleted.Clear();
        }

        public void StartStep(Guid sectionId, string pageName, bool clearRequirementsCompleted = false)
        {
            SectionId = sectionId;
            PageName = pageName;
            DraftAudioIds.Clear();

            if (clearRequirementsCompleted)
            {
                RequirementsCompleted.Clear();
            }

            CurrentDraftAudioId = Guid.Empty;
            PreviousDraftId = Guid.Empty;
        }

        public void SetPassageNumber(PassageNumber passageNumber)
        {
            PassageNumber = passageNumber;
        }

        public bool AddDraftAudioId(Guid draftAudioId)
        {
            if (DraftAudioIds.Contains(draftAudioId))
            {
                return false;
            }
            
            DraftAudioIds.Add(draftAudioId);
            CurrentDraftAudioId = draftAudioId;

            return true;
        }
        
        public void RemoveDraftAudioId(Guid audioId)
        {
            DraftAudioIds.Remove(audioId);
        }
        
        public void SetNonDraftTranslationId(Guid id)
        {
            NonDraftTranslationId = id;
        }

        public bool RequirementMetInSession(Guid requirementId)
        {
            return RequirementsCompleted.Any(x => 
                x.PageName == PageName && 
                PassageNumber.BothAreNullOrEqual(x.PassageNumber, PassageNumber) && 
                (x.NonDraftTranslationId == default || x.NonDraftTranslationId == NonDraftTranslationId) &&
                x.RequirementId == requirementId);
        }

        public void AddRequirementCompletion(Guid requirementId)
        {
            RequirementsCompleted.Add(new SessionRequirement(PageName, PassageNumber, NonDraftTranslationId, requirementId));
        }

        private const int Version = 3;

        public string CheckForCurrentStep(Guid stepId, Guid sectionId, PassageNumber passageNumber)
        {
            var isCurrent =  StepId == stepId
                && SectionId == sectionId
                && (PassageNumber is null || PassageNumber.Equals(passageNumber));
            if (!isCurrent)
            {
                //TODO : do something when current is false
            }
            return isCurrent ? PageName : "";
        }

        public void SetPageName(string pageName)
        {
            PageName = pageName;
        }

        public void ResetAudios()
        {
            DraftAudioIds.Clear();
            CurrentDraftAudioId = Guid.Empty;
        }
    }
}