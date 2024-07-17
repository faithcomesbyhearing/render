using Newtonsoft.Json;
using Render.Models.Audio;

namespace Render.Models.Sections.CommunityCheck
{
    public class CommunityRetell : NotableAudio
    {
        [JsonProperty("StageId")]
        public Guid StageId { get; }
        
        public CommunityRetell(Guid stageId, Guid scopeId, Guid projectId, Guid parentId) 
            : base(scopeId, projectId, parentId)
        {
            StageId = stageId;
        }
    }
}