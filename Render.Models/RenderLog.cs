using Couchbase.Linq.Filters;
using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;

namespace Render.Models
{
    [DocumentTypeFilter("vesselLog")]
    public class RenderLog : DomainEntity, IAggregateRoot, IDataStorage
    {
        [JsonProperty("LogLines")]
        public List<string> LogLines { get; set; }

        public RenderLog(List<string> logLines)
            :base(0)
        {
            LogLines = logLines;
        }
    }
}