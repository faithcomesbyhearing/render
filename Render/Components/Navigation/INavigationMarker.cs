using Render.Components.NotePlacementPlayer;
using Render.TempFromVessel.Kernel;

namespace Render.Components.Navigation
{
    public interface INavigationMarker
    {
        DomainEntity Item { get; }

        FlagState FlagState { get; }
    }
}