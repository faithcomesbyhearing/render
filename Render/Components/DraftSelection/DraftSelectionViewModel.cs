using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.BarPlayer;
using Render.Kernel;

namespace Render.Components.DraftSelection
{
    public enum DraftSelectionState
    {
        Selected,
        Unselected,
        Required
    }
    
    public class DraftSelectionViewModel : ActionViewModelBase, IDraftSelectionViewModel
    {
        public IBarPlayerViewModel MiniWaveformPlayerViewModel { get; set; }
        
        public ReactiveCommand<Unit, Unit> SelectCommand { get; }

        [Reactive]
        public DraftSelectionState DraftSelectionState { get; set; }
        
        public DraftSelectionViewModel(
            IBarPlayerViewModel miniWaveformPlayerViewModel, 
            IViewModelContextProvider viewModelContextProvider, 
            ActionState actionState) 
            : base(actionState,"DraftSelection", viewModelContextProvider)
        {
            MiniWaveformPlayerViewModel = miniWaveformPlayerViewModel;
            DraftSelectionState = DraftSelectionState.Required;
        
            SelectCommand = ReactiveCommand.Create(SelectDraft);
        }

        private void SelectDraft()
        {
            DraftSelectionState = DraftSelectionState.Selected;
            LogInfo("Selected Draft", new Dictionary<string, string>()
            {
                {"Draft Name", MiniWaveformPlayerViewModel.AudioTitle}
            });
        }
        
        public override void Dispose()
        {
            MiniWaveformPlayerViewModel.Dispose();

            base.Dispose();
        }
    }
}