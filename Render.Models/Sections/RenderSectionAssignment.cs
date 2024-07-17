using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Sections
{
    public class RenderSectionAssignment : ValueObject
    {
        [JsonProperty("RoleId")]
        public Guid RoleId { get; }

        [JsonProperty("UserId")]
        public Guid UserId { get; }

        public RenderSectionAssignment(Guid roleId, Guid userId)
        {
            RoleId = roleId;
            UserId = userId;
        }
    }
}