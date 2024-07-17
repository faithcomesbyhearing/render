using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using ReactiveUI.Fody.Helpers;
using Render.Models.Workflow;

namespace Render.Components.Modal.ModalComponents
{
    public class StageDeleteConfirmationComponentViewModel : ViewModelBase
    {
        [Reactive]
        public StageState StageState { get; set; } 
        
        public ReactiveCommand<Unit, bool> ContinueCommand { get; }

        public StageDeleteConfirmationComponentViewModel(IViewModelContextProvider viewModelContextProvider)
            : base(string.Empty, viewModelContextProvider)
        {
            ContinueCommand = ReactiveCommand.Create(() => true,
                this.WhenAnyValue(x => x.StageState)
                    .Select(x => x is StageState.CompleteWork or StageState.RemoveWork));
        }
    }
}
