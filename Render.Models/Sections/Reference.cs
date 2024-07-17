using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Sections
{
    public class Reference : ProjectDomainEntity
    {
        [JsonProperty("Name")]
        public string Name { get; private set; }
        
        [JsonProperty("Language")]
        public string Language { get; private set; }

        [JsonProperty("MediaId")]
        public string MediaId { get; private set; }
        
        [JsonProperty("Primary")]
        public bool Primary { get; private set; }

        [JsonProperty("BibleVersionId")]
        public Guid BibleVersionId { get; private set; }

        public Reference(string name, string language, string mediaId, bool primary, Guid projectId, Guid bibleVersionId)
            : base(projectId, Version)
        {
            Name = name;
            Language = language;
            MediaId = mediaId;
            Primary = primary;
            BibleVersionId = bibleVersionId;
        }

        public void UpdateReferenceInformation(string name, bool required)
        {
            Name = name;
            Primary = required;
        }
        
        private const int Version = 4;
    }
}