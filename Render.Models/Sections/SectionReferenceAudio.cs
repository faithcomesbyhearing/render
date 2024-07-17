using Newtonsoft.Json;

namespace Render.Models.Sections
{
    public class SectionReferenceAudio : Audio.Audio
    {
        private const int Version = 0;

        //Parent id should be the section id
        public SectionReferenceAudio(Guid projectId, Guid referenceId, Guid parentId, Guid scopeId) : base
        (scopeId, projectId, parentId)
        {
            ReferenceId = referenceId;
            LockedReferenceByPassageNumbersList = new List<int>();
        }

        [JsonProperty("ReferenceId")]
        public Guid ReferenceId { get; }

        [JsonProperty("PassageReferences")]
        public List<PassageReference> PassageReferences { get; private set; } = new List<PassageReference>();

        /// <summary>
        /// Duration of the audio in millisecond
        /// </summary>
        [JsonProperty("AudioDuration")]
        public int AudioDuration { get; private set; }

        [JsonProperty("FailedVerification")]
        public bool FailedVerification { get; private set; }

        /// <summary>
        /// List of Int that represent the set of passages numbers to be excluded from showing in the drafting and peer check stage of the workflow 
        /// </summary>
        /// <returns></returns>
        [JsonProperty("LockedReferenceByPassageNumbersList")]
        public List<int> LockedReferenceByPassageNumbersList { get; set; }

        /// <summary>
        /// This is not an editable Reference, and should be used as a read-only object.
        /// </summary>
        [JsonIgnore]
        public Reference Reference { get; set; }

        public void SetPassageReferences(List<PassageReference> passageReferences)
        {
            PassageReferences = passageReferences;
        }

        public PassageReference GetPassageReference(int passageNumber)
        {
            return PassageReferences.FirstOrDefault(x => x.PassageNumber.Number == passageNumber);
        }

        public void ChangePassageReference(PassageReference passageReference)
        {
            var passageReferenceToDelete = PassageReferences.FirstOrDefault(x => x.PassageNumber == passageReference.PassageNumber);
            PassageReferences.Remove(passageReferenceToDelete);
            PassageReferences.Add(passageReference);
        }

        public void ResetPassageReferencesWithDivisions()
        {
            if (PassageReferences.Any(x => x.PassageNumber.DivisionNumber > 0))
            {
                var distinctPassages = PassageReferences.GroupBy(pr => pr.PassageNumber.Number)
                                                        .Select(g => g.Last())
                                                        .OrderBy(gpr => gpr.PassageNumber.Number)
                                                        .ToList();

                var result = new List<PassageReference>();
                for (var i = 0; i < distinctPassages.Count; i++)
                {
                    var passageReference = distinctPassages[i];
                    var passageNumber = new PassageNumber(distinctPassages[i].PassageNumber.Number);

                    int startMarkerTime = i == 0
                                            ? 0
                                            : result[i - 1].TimeMarkers.EndMarkerTime;

                    int endMarkerTime = distinctPassages[i].TimeMarkers.EndMarkerTime;
                    var timeMarkers = new TimeMarkers(startMarkerTime, endMarkerTime);

                    var newPassageReference = new PassageReference(passageNumber, timeMarkers);

                    result.Add(newPassageReference);
                }

                PassageReferences = result;
            }
        }
    }
}