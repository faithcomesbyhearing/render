using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Project
{
    public class Language : DomainEntity, IAggregateRoot
    {
        public Language () : base(Version)
        {
            EnglishLanguageName = "";
            Autonym = "";
            Description = "";
            DomainEntityVersion = Version;
        }

        public Language (string englishLanguageName, string autonym, string description) :base(Version)
        {
            EnglishLanguageName = englishLanguageName;
            Autonym = autonym;
            Description = description;
            DomainEntityVersion = Version;
        }

        [JsonProperty("EnglishLanguageName")]
        public string EnglishLanguageName { get; private set; }

        [JsonProperty("Autonym")]
        public string Autonym { get; private set; }

        [JsonProperty("Description")]
        public string Description { get; private set; }

        [JsonProperty("IsDeleted")]
        public bool IsDeleted { get; private set; }
        
        public string GetLanguageName()
        {
            return string.IsNullOrWhiteSpace(Autonym) ? EnglishLanguageName : Autonym;
        }

        public void SetEnglishLanguageName(string name)
        {
            EnglishLanguageName = name;
        }

        public void SetAutonym(string name)
        {
            Autonym = name;
        }

        // Set the "soft" delete flag
        public void ToggleIsDeleted()
        {
            IsDeleted = !IsDeleted;
        }

        private const int Version = 2;
    }
}