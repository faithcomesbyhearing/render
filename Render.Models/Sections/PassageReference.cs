using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Sections
{
    public class PassageReference : ValueObject
    {
        [JsonProperty("PassageNumber")]
        public PassageNumber PassageNumber { get; private set; }
        
        [JsonProperty("TimeMarkers")]
        public TimeMarkers TimeMarkers { get; }

        public PassageReference(PassageNumber passageNumber, TimeMarkers timeMarkers)
        {
            PassageNumber = passageNumber;
            TimeMarkers = timeMarkers;
        }
    }
}