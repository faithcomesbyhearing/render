using Newtonsoft.Json;
using Render.Models.Scope;
using Render.Models.Sections;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Snapshot
{
    /// <summary>
    /// The state of a section at the point in time when a stage is completed.
    /// Acts as a historical record of the section at the end of this stage in the translation process.
    /// </summary>
    public class Snapshot : ScopeDomainEntity, IAggregateRoot, IEquatable<Snapshot>
    {
        public static Snapshot UnitTestEmptySnapshot(Guid sectionId = default, Section section = null, string stageName = "", Guid stageId = default, Guid stepId = default)
        {
            if (section == null)
            {
                section = Section.UnitTestEmptySection();
            }
            var snapshot = new Snapshot(
                sectionId == default ? section.Id : sectionId,
                Guid.Empty, Guid.Empty, section.ApprovedDate, Guid.Empty, section.ScopeId, section.ProjectId, stageId,
                stepId, section.Passages, new List<SectionReferenceAudioSnapshot>(), new List<Guid>(), stageName);
            return snapshot;
        }

        [JsonProperty("StageId")] public Guid StageId { get; }

        [JsonProperty("StepId")] public Guid StepId { get; }

        [JsonProperty("SectionId")] public Guid SectionId { get; private set; }

        [JsonProperty("Passages")] public List<Passage> Passages { get; } = new List<Passage>();

        [JsonProperty("SectionReferenceAudioSnapshots")]
        public List<SectionReferenceAudioSnapshot> SectionReferenceAudioSnapshots { get; private set; }

        [JsonProperty("NoteInterpretationIds")]
        public List<Guid> NoteInterpretationIds { get; private set; }

        [JsonProperty("ApprovedBy")] public Guid ApprovedBy { get; private set; }

        [JsonProperty("ApprovedDate")] public DateTimeOffset ApprovedDate { get; private set; }

        [JsonProperty("CheckedBy")] public Guid CheckedBy { get; private set; }

        [JsonProperty("CreatedBy")] public Guid CreatedBy { get; private set; }

        // audio revisions within a stage
        [JsonProperty("Temporary")] public bool Temporary { get; private set; }

        // deleted permanent snapshot - each stage has one permanent snapshot when it finished
        [JsonProperty("Deleted")] public bool Deleted { get; private set; }

        [JsonProperty("StageName")] public string StageName { get; }

        [JsonProperty("PassageBackTranslations")]
        public List<PassageBackTranslation> PassageBackTranslations { get; } = new List<PassageBackTranslation>();
        
        [JsonProperty("PassageDrafts")]
        public List<PassageDraft> PassageDrafts { get; } = new();
        
        //Denotes the snapshot that preceded this one in the history of the section. This snapshot was built off of the
        //snapshot that came before it. This should be an empty guid for temporary snapshots
        [JsonProperty("ParentSnapshot")] public Guid ParentSnapshot { get; private set; }

        public Snapshot(
            Guid sectionId,
            Guid checkedBy,
            Guid approvedBy,
            DateTimeOffset approvedDate,
            Guid createdBy,
            Guid scopeId,
            Guid projectId,
            Guid stageId,
            Guid stepId,
            List<Passage> passages,
            List<SectionReferenceAudioSnapshot> sectionReferenceAudioSnapshots,
            List<Guid> noteInterpretationIds,
            string stageName,
            Guid parentSnapshot = default,
            bool temporary = false) : base(scopeId, projectId, 2)
        {
            StageId = stageId;
            StepId = stepId;
            SectionId = sectionId;
            CheckedBy = checkedBy;
            ApprovedBy = approvedBy;
            ApprovedDate = approvedDate;
            CreatedBy = createdBy;
            ParentSnapshot = parentSnapshot;
            NoteInterpretationIds = noteInterpretationIds;
            Passages.AddRange(passages);
            SectionReferenceAudioSnapshots = new List<SectionReferenceAudioSnapshot>();

            if (sectionReferenceAudioSnapshots != null)
            {
                SectionReferenceAudioSnapshots.AddRange(sectionReferenceAudioSnapshots);
            }

            Temporary = temporary;
            StageName = stageName;

            foreach (var passage in passages)
            {
                if (!passage.HasAudio) continue;
                
                var retellId = passage.CurrentDraftAudio.RetellBackTranslationAudio?.Id ?? Guid.Empty;
                var segmentIds = passage.CurrentDraftAudio.SegmentBackTranslationAudios.Select(segment => segment.Id).ToList();
                PassageDrafts.Add(new PassageDraft(passage.Id, passage.CurrentDraftAudio.Id));   
                PassageBackTranslations.Add(new PassageBackTranslation(passage.Id, retellId, segmentIds));
            }
        }

        public void SetDelete(bool delete)
        {
            Temporary = delete;
        }
        public void SetDeleted(bool deleted)
        {
            Deleted = deleted;
        }

        public bool HasPassage(PassageNumber passageNumber)
        {
            return Passages.Any(x => x.PassageNumber.Number.Equals(passageNumber.Number));
        }

        public override string ToString()
        {
            return $"{StageName}: {DateUpdated.LocalDateTime}";
        }

        public bool Equals(Snapshot other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return
               StageId.Equals(other.StageId) &&
               SectionId.Equals(other.SectionId) &&
               Equals(Passages, other.Passages) &&
               Equals(SectionReferenceAudioSnapshots, other.SectionReferenceAudioSnapshots) &&
               ApprovedBy.Equals(other.ApprovedBy) &&
               CheckedBy.Equals(other.CheckedBy) &&
               Temporary == other.Temporary &&
               StageName == other.StageName;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            return Equals((Snapshot)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = StageId.GetHashCode();
                hashCode = (hashCode * 397) ^ SectionId.GetHashCode();
                hashCode = (hashCode * 397) ^ (Passages != null ? Passages.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SectionReferenceAudioSnapshots != null ? SectionReferenceAudioSnapshots.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ApprovedBy.GetHashCode();
                hashCode = (hashCode * 397) ^ CheckedBy.GetHashCode();
                hashCode = (hashCode * 397) ^ Temporary.GetHashCode();
                hashCode = (hashCode * 397) ^ (StageName != null ? StageName.GetHashCode() : 0);
                return hashCode;
            }
        }

        public void SetSectionId(Guid sectionId)
        {
            SectionId = sectionId;
        }

        public bool DateTimeOffsetWithinRange(int timespanInSeconds, DateTimeOffset dateTime)
        {
            var diff = DateUpdated.LocalDateTime - dateTime.LocalDateTime;
            return diff.Duration() < TimeSpan.FromSeconds(timespanInSeconds);
        }
    }
}