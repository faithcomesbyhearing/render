using Newtonsoft.Json;
using ReactiveUI;

namespace Render.TempFromVessel.Kernel
{
    /// <summary>
    /// Provides common functionality for DDD value objects.
    /// </summary>
    public abstract class ValueObject : ReactiveObject
    {
        [JsonProperty("EquivalenceValue")]
        public Guid EquivalenceValue { get; private set; } = Guid.NewGuid();
    }
}