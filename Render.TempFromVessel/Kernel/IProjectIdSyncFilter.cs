using Newtonsoft.Json;

namespace Render.TempFromVessel.Kernel
{
    public interface IProjectIdSyncFilter
    {
        [JsonProperty("ProjectId")]
        Guid ProjectId { get; }
    }
}
