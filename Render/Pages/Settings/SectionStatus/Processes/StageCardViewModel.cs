using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Workflow.Stage;

namespace Render.Pages.Settings.SectionStatus.Processes
{
    public class StageCardViewModel : ViewModelBase
    {
        public Stage Stage { get; private set; }
        [Reactive]
        public string Glyph { get; private set; }
        public ObservableCollection<StepCardViewModel> StepCards { get; private set; }
        public ReactiveCommand<Unit, Unit> ToggleStepsCommand;

        [Reactive] public bool ShowSteps { get; set; }
        
        public StageCardViewModel(Stage stage, List<StepCardViewModel> stepCards, 
            IViewModelContextProvider viewModelContextProvider) 
            : base("SectionStatusStageCard", viewModelContextProvider)
        {

            try
            {
                Stage = stage;
                Glyph = IconMapper.GetIconForStageType(stage.StageType, true)?.Glyph;
                StepCards = new ObservableCollection<StepCardViewModel>(stepCards);
                ToggleStepsCommand = ReactiveCommand.Create(ToggleSteps);
            }
            catch (Exception ex)
            {
                var message = ex.Message;
            }
            
        }

        private void ToggleSteps()
        {
            ShowSteps = !ShowSteps;
        }

        public override void Dispose()
        {
            foreach (var stepCardViewModel in StepCards)
            {
                stepCardViewModel.Dispose();
            }
            StepCards?.Clear();

            Stage = null;

            ToggleStepsCommand?.Dispose();
            ToggleStepsCommand = null;

            base.Dispose();
        }
    }
}