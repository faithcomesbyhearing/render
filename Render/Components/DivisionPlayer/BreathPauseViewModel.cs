using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;

namespace Render.Components.DivisionPlayer
{
    public class BreathPauseViewModel : ViewModelBase
    {
        [Reactive]
        public double ChunkDuration { get; private set; }

        [Reactive]
        public bool IsDivisionMarker { get; set; }
        
        [Reactive]
        public int Position { get; private set; }
        
        [Reactive]
        public double Scale { get; set; }
        
        public ReactiveCommand<Unit, Unit> ChangeDivisionStateCommand { get; }

        public BreathPauseViewModel(IViewModelContextProvider viewModelContextProvider, int position,
            double chunkDuration) : base("BreathPause", viewModelContextProvider)
        {
            Position = position;
            ChunkDuration = chunkDuration;

            ChangeDivisionStateCommand = ReactiveCommand.Create(ChangeMarkerState);
        }

        private void ChangeMarkerState()
        {
            IsDivisionMarker = !IsDivisionMarker;
        }
    }
}