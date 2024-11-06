using Newtonsoft.Json;
using Render.Models.Sections.CommunityCheck;
using ReactiveUI.Fody.Helpers;
using Render.Models.Audio;

namespace Render.Models.Sections
{
    public class Draft : NotableAudio
    {
        [JsonProperty("Name")]
        public string Name { get; private set; }

        [JsonProperty("Transcription")]
        public string Transcription { get; private set; }

        [JsonProperty("CreatedById")]
        public Guid CreatedById { get; private set; }

        [JsonProperty("CreatedByName")]
        public string CreatedByName { get; private set; }

        [JsonProperty("Revision")]
        public int Revision { get; private set; }

        [JsonProperty("Deleted")]
        public bool Deleted { get; private set; }

        [JsonIgnore]
        [Reactive]
        public RetellBackTranslation RetellBackTranslationAudio { get; set; }

        [JsonIgnore]
        public List<SegmentBackTranslation> SegmentBackTranslationAudios { get; set; } = new List<SegmentBackTranslation>();

        [JsonIgnore] public bool HasCommunityTest => CommunityTest != null;

        [JsonIgnore]
        private CommunityTest CommunityTest { get; set; }

        public Draft(
            Guid scopeId,
            Guid projectId,
            Guid parentId,
            int documentVersion = 2)
            : base(
                scopeId,
                projectId,
                parentId,
                documentVersion)
        {
        }

        public void SetTranscription(string transcription)
        {
            Transcription = transcription;
        }

        public CommunityTest GetCommunityCheck()
        {
            return CommunityTest ??= new CommunityTest(Id, ScopeId, ProjectId);
        }

        public void SetCommunityTest(CommunityTest test)
        {
            CommunityTest = test;
        }

        public void SetRevision(int revision)
        {
            Revision = revision;
        }

        public void SetCreatedBy(Guid creatorId, string creatorName)
        {
            CreatedById = creatorId;
            CreatedByName = creatorName;
        }

        public void SetDeleted(bool deleted)
        {
            Deleted = deleted;
        }
    }
}