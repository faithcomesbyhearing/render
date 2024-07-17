using Newtonsoft.Json;

namespace Render.Models.Sections
{
    public class SupplementaryMaterial : Media
    {
        [JsonProperty("Name")] 
        public string Name { get; }

        public SupplementaryMaterial(string name, Guid audioId = default, string text = "") : base(audioId, text)
        {
            Name = name;
        }
    }
}