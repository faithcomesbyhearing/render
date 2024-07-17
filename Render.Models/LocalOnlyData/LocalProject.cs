using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;

namespace Render.Models.LocalOnlyData
{
    public class LocalProject: ValueObject
    {
        [JsonProperty("ProjectId")]
        public Guid ProjectId { get; set; }
        
        [JsonProperty("State")]
        public DownloadState State { get; set; }
        
        [JsonProperty("LastSyncDate")]
        public DateTime LastSyncDate { get; set; }
    }
}