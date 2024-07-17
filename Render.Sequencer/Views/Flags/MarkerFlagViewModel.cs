using ReactiveUI.Fody.Helpers;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Views.Flags.Base;

namespace Render.Sequencer.Views.Flags;

public class MarkerFlagViewModel : BaseFlagViewModel, IMarkerFlag
{
    /// <summary>
    /// Short 1-2 symbols string. Usually number.
    /// </summary>
    [Reactive]
    public string? Symbol { get; set; }

    public MarkerFlagViewModel()
    {
        IconKey = "Flag";
    }

    /// <summary>
    /// Update PositionDip to prevent collision
    /// </summary>
    public void SetPositionDip(double width)
    {
        PositionDip = FlagDirectionHelper.GetFlagPosition(PositionDip, width);
    }
}