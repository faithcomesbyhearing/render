using System;
using Newtonsoft.Json;

namespace Render.TempFromVessel.Kernel
{
    public interface IDataStorage
    {
        [JsonProperty("DateUpdated")]
        DateTimeOffset DateUpdated { get; }

        [JsonProperty("Type")]
        string Type { get; }
    }
}
