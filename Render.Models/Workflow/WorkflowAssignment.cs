using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Workflow
{
    public class WorkflowAssignment : ValueObject
    {
        [JsonProperty("UserId")]
        public Guid UserId { get; }
        
        [JsonProperty("StageId")]
        public Guid StageId { get; }
        
        [JsonProperty("StageType")]
        public StageTypes StageType { get; }
        
        [JsonProperty("Role")]
        public Roles Role { get; }

        public WorkflowAssignment(Guid userId, Guid stageId, StageTypes stageType, Roles role)
        {
            UserId = userId;
            StageId = stageId;
            StageType = stageType;
            Role = role;
        }
    }
}