using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.Navigation;
using Render.Components.NotePlacementPlayer;
using Render.Kernel;
using Render.Models.Sections.CommunityCheck;
using Render.TempFromVessel.Kernel;

namespace Render.Components.CommunityTestFlagLibraryModal
{
    public class CommunityTestFlagMarkerViewModel : ActionViewModelBase, INavigationMarker
    {
        public Flag Flag { get; set; }
        
        [Reactive]
        public double Width { get; set; }

        public DomainEntity Item => Flag;

        [Reactive]
        public FlagState FlagState { get; set; }

        [Reactive]
        public int ItemsCount { get; private set; }
        
        public CommunityTestFlagMarkerViewModel(
            IViewModelContextProvider viewModelContextProvider, 
            Flag flag, 
            FlagState flagState = FlagState.Optional,
            int retellsCount = 0) : base(ActionState.Optional, "CommunityCheckFlagMarkerViewModel", viewModelContextProvider)
        {
            Flag = flag;
            Width = Flag.TimeMarker;
            FlagState = flagState;
            
            Disposables.Add(this
                .WhenAnyValue(vm => vm.FlagState)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    ActionState = FlagState == FlagState.Required ? ActionState.Required : ActionState.Optional;
                }));

            Disposables.Add(this
                .WhenAnyValue(vm => vm.Flag.QuestionCount)
                .Subscribe(questionCount =>
                {
                    ItemsCount = questionCount + retellsCount;
                }));
        }

        public override void Dispose()
        {
            Flag = null;

            base.Dispose();
        }
    }
}