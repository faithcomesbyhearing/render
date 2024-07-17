using Render.Sequencer.Contracts.Enums;

namespace Render.Sequencer.Contracts.Models;

public record NoteFlagModel : FlagModel
{
    public FlagDirection Direction { get; set; }

    public NoteFlagModel(double position, bool required, bool read)
        : base(position, required, read) { }

    public NoteFlagModel(Guid key, double position, bool required, bool read)
        : base(key, position, required, read) { }
}