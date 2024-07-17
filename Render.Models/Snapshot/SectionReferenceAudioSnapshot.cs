using Newtonsoft.Json;
using Render.Models.Sections;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Snapshot
{
    public class SectionReferenceAudioSnapshot : ValueObject
    {
        [JsonProperty("SectionReferenceAudioId")]
        public Guid SectionReferenceAudioId { get; }

        [JsonProperty("PassageReferences")]
        public List<PassageReference> PassageReferences { get; } = new List<PassageReference>();

        /// <summary>
        /// List of Int that represent the set of passages numbers to be excluded from showing in the drafting and peer check stage of the workflow 
        /// </summary>
        /// <returns></returns>
        [JsonProperty("LockedReferenceByPassageNumbersList")]
        public List<int> LockedReferenceByPassageNumbersList { get; set; }

        public SectionReferenceAudioSnapshot(Guid sectionReferenceAudioId, List<int> lockedReferenceByPassageNumbersList,
            List<PassageReference> passageReferences = null)
        {
            SectionReferenceAudioId = sectionReferenceAudioId;
            if (passageReferences != null)
            {
                PassageReferences.AddRange(passageReferences);
            }
            LockedReferenceByPassageNumbersList = lockedReferenceByPassageNumbersList;
        }
    }
}