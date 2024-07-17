using Newtonsoft.Json;
using ReactiveUI.Fody.Helpers;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Sections
{
    public class Passage : DomainEntity
    {
        [Obsolete("Use CurrentDraftAudio.Id instead")]
        [JsonProperty("CurrentPassageAudioId")]
        public Guid CurrentDraftAudioId { get; private set; }
        
        [JsonProperty("PassageNumber")] public PassageNumber PassageNumber { get; private set; }

        [JsonProperty("SupplementaryMaterials")]
        public IReadOnlyCollection<SupplementaryMaterial> SupplementaryMaterials { get; private set; } =
            new List<SupplementaryMaterial>();

        [JsonProperty("ScriptureReferences")] public List<ScriptureReference> ScriptureReferences { get; private set; }

        [JsonIgnore] public bool HasSupplementaryMaterial => SupplementaryMaterials.Any();

        [JsonIgnore] public bool HasAudio => CurrentDraftAudio != null;

        [Reactive] [JsonIgnore] public Draft CurrentDraftAudio { get; set; }
        
        public Passage(PassageNumber passageNumber) : base(documentVersion: 1)
        {
            PassageNumber = passageNumber;
            ScriptureReferences = new List<ScriptureReference>();
        }

        public void ChangeCurrentDraftAudio(Draft audio)
        {
            if (audio != null)
            {
                CurrentDraftAudio = audio;
                CurrentDraftAudioId = audio.Id;
            }
        }        
        
        public void RemoveCurrentDraftAudio()
        {
            CurrentDraftAudioId = Guid.Empty;
            CurrentDraftAudio = null;
        }
        public void SetPassageNumber(PassageNumber passageNumber)
        {
            PassageNumber = passageNumber;
        }

		public void SetScriptureReferences(List<ScriptureReference> scriptureReferences)
		{
			ScriptureReferences = scriptureReferences;
		}

		public void SetSupplementaryMaterial(List<SupplementaryMaterial> supplementaryMaterials)
        {
            SupplementaryMaterials = supplementaryMaterials;
        }

        public bool Equals(Passage other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(PassageNumber, other.PassageNumber) &&
                   Equals(SupplementaryMaterials, other.SupplementaryMaterials) &&
                   HasSupplementaryMaterial == other.HasSupplementaryMaterial;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Passage)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = CurrentDraftAudio != null ? CurrentDraftAudio.Id.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (PassageNumber != null ? PassageNumber.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^
                           (SupplementaryMaterials != null ? SupplementaryMaterials.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ HasSupplementaryMaterial.GetHashCode();
                return hashCode;
            }
        }
    }
}