using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;
using Render.TempFromVessel.Project;
using Render.TempFromVessel.User;
using Reference = Render.Models.Sections.Reference;

namespace Render.Models.Project
{
	public class RenderProject: ProjectDomainEntity, IAggregateRoot, IProjectIdSyncFilter, IWorkProject
	{
        // Added a default constructor as required for JSON Serialization,
        // even though we don't use this
        public RenderProject()
            : base(Guid.Empty, Version)
        {
            CreateRoleIdList();
            DomainEntityVersion = Version;
        }

        public RenderProject(Guid projectId)
            : base(projectId, Version)
        {
            CreateRoleIdList();
            DomainEntityVersion = Version;
        }

        // Constructor added for Data Context test data
        public RenderProject(Guid projectId, Language language, string pseudonym, Guid countryId, string region, 
            bool isTrainingProject, Sensitivity projectSensitivity) : base(projectId, Version)
        {
            RenderProjectLanguageId = language.Id;
            EnglishLanguageName = language.EnglishLanguageName;
            Autonym = language.Autonym;
            Pseudonym = pseudonym;
            Region = region;
            CountryId = countryId;
            IsTrainingProject = isTrainingProject;
            ProjectSensitivity = projectSensitivity;
            CreateRoleIdList();
            DomainEntityVersion = Version;
        }

        [JsonProperty("ProjectRoleIds")]
        public List<Guid> ProjectRoleIds { get; private set; }

        private void CreateRoleIdList()
        {
            ProjectRoleIds = new List<Guid>
            {
                RoleName.General.GetRoleId(),
                RoleName.Configure.GetRoleId(),
                RoleName.Consultant.GetRoleId()
            };
        }

        [JsonProperty("ReferenceIds")]
        public List<Guid> ReferenceIds { get; set; }
        
        [JsonProperty("CountryId")]
        public Guid CountryId { get; private set; }

        [JsonProperty("CountryName")]
        public string CountryName{ get; private set;}

        [JsonProperty("RenderProjectLanguageId")]
        public Guid RenderProjectLanguageId { get; private set; }

        [JsonProperty("EnglishLanguageName")]
        public string EnglishLanguageName { get; private set; }

        [JsonProperty("Autonym")]
        public string Autonym { get; private set; }

        [JsonProperty("Pseudonym")]
        public string Pseudonym { get; private set; }

        [JsonProperty("Region")]
        public string Region { get; private set; }

        [JsonProperty("IsTrainingProject")]
        public bool IsTrainingProject { get; private set; }
        
        [JsonProperty("ProjectSensitivity")]
        public Sensitivity ProjectSensitivity { get; private set; }

        [JsonProperty("EndDate")]
        public DateTimeOffset EndDate { get; private set;}

		[JsonProperty("IsBetaTester")]
		public bool IsBetaTester { get; private set; }

		[JsonIgnore]
        public List<Scope.Scope> Scopes { get; }
            = new List<Scope.Scope>();

        [JsonIgnore]
        public List<Reference> References { get; }

        public void SetRenderProjectLanguageId(Guid languageId)
        {
            RenderProjectLanguageId = languageId;
        }
        
        public void SetLanguage(string englishName = "", string autonym = "", string pseudonym = "")
        {
            EnglishLanguageName = englishName;
            Autonym = autonym;
            Pseudonym = pseudonym;
        }

        public void SetEnglishLanguageName(string englishLanguageName)
        {
            EnglishLanguageName = englishLanguageName;
        }

        public void SetAutonym(string autonym)
        {
            Autonym = autonym;
        }

        public void SetPseudonym(string pseudonym)
        {
            Pseudonym = pseudonym;
        }

        public void SetRegion(string region)
        {
            Region = region;
        }

        public void SetTrainingProject(bool trainingProject)
        {
            IsTrainingProject = trainingProject;
        }
        
        public string GetLanguageName()
        {
            return string.IsNullOrWhiteSpace(Autonym) ? EnglishLanguageName : Autonym;
        }

        private const int Version = 6;
    }
}