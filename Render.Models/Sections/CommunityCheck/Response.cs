using Newtonsoft.Json;
using Render.Models.Audio;

namespace Render.Models.Sections.CommunityCheck
{
    public class Response : NotableAudio
    {
        [JsonProperty("StageId")]
        public Guid StageId { get; set; }
        
        public Response(Guid stageId, Guid scopeId, Guid projectId, Guid parentId) : base(scopeId, projectId, parentId)
        {
            StageId = stageId;
        }
    }
}