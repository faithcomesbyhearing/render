using System.Reactive;
using ReactiveUI;
using Render.Components.BarPlayer;
using Render.Kernel;

namespace Render.Components.DraftSelection
{
    public interface IDraftSelectionViewModel : IActionViewModelBase
    {
        DraftSelectionState DraftSelectionState { get; set; }
        ReactiveCommand<Unit, Unit> SelectCommand { get; }
        IBarPlayerViewModel MiniWaveformPlayerViewModel { get; set; }
    }
}