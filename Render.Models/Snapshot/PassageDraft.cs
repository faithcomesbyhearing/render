using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Snapshot;

public class PassageDraft : ValueObject
{
    [JsonProperty("PassageId")] public Guid PassageId { get; }
    [JsonProperty("DraftId")] public Guid DraftId { get; }

    public PassageDraft(Guid passageId, Guid draftId)
    {
        PassageId = passageId;
        DraftId = draftId;
    }
}