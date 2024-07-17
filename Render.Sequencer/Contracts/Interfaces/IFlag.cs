using Render.Sequencer.Contracts.Enums;

namespace Render.Sequencer.Contracts.Interfaces;

public interface IFlag
{
    Guid Key { get; set; }
    
    double PositionSec { get; }
    
    public FlagState State { get; set; }

    public ItemOption Option { get; set; }
}