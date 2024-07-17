using Newtonsoft.Json;
using Render.Models.Sections;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Scope
{
    public class Scope : ProjectDomainEntity, IAggregateRoot, IProjectIdSyncFilter
    {
        [JsonProperty("Name")]
        public string Name { get; private set; } = "";

        [JsonProperty("ProjectedStartYear")]
        public int ProjectedStartYear { get; private set;}

        [JsonProperty("ProjectedStartMonth")]
        public int ProjectedStartMonth { get; private set;}

        [JsonProperty("Status")]
        public string Status { get; private set;} = "Inactive";

        [JsonProperty("DeployedDate")]
        public DateTimeOffset DeployedDate { get; private set;}

        [JsonProperty("FirstSectionDraftedDate")]
        public DateTimeOffset FirstSectionDraftedDate { get; private set; }

        [JsonProperty("FinalSectionApprovedDate")]
        public DateTimeOffset FinalSectionApprovedDate { get; private set; }
        
        [JsonIgnore]
        public List<Section> Sections { get; } = new List<Section>();

        public Scope(Guid projectId) : base(projectId, Version)
        {
            DomainEntityVersion = Version;
        }

        //Organization ids for partners included on the scope
        public List<Guid> IncludedPartnerIds { get; set; } = new List<Guid>();

        //Organization ids for primary partners on the scope
        public List<Guid> PrimaryPartnerIds { get; set; } = new List<Guid>();

        //Templates added to the scope
        public List<Guid> ImportedFromTemplateIds { get; set; } = new List<Guid>();

        public void SetName(string name)
        {
            Name = name;
        }
        
        public void SetProjectedStartYear(int projectedStartYear)
        {
            ProjectedStartYear = projectedStartYear;
        }
        
        public void SetProjectedStartMonth(int projectedStartMonth)
        {
            ProjectedStartMonth = projectedStartMonth;
        }
        
        public void SetStatus(string status)
        {
            Status = status;
        }
        
        public void SetDeployedDate(DateTimeOffset deployedDate)
        {
            DeployedDate = deployedDate;
        }
        
        public void SetFirstSectionDraftedDate(DateTimeOffset firstSectionDraftedDate)
        {
            FirstSectionDraftedDate = firstSectionDraftedDate;
        }
        
        public void SetFinalSectionApprovedDate(DateTimeOffset finalSectionApprovedDate)
        {
            FinalSectionApprovedDate = finalSectionApprovedDate;
        }

        private const int Version = 2;
    }
}