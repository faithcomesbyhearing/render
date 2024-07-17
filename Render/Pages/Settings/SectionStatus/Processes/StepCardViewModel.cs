using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Workflow;

namespace Render.Pages.Settings.SectionStatus.Processes
{
    public class StepCardViewModel : ViewModelBase
    {
        public Step Step { get; private set; }
        public string StepName { get; }
        public ObservableCollection<SectionCardViewModel> SectionCards { get; private set; }
        public ReactiveCommand<Unit, Unit> ToggleSectionsCommand;

        [Reactive] public bool ShowSections { get; set; }
        
        [Reactive] public bool LastStepCard { get; set; }

        public StepCardViewModel(Step step, List<SectionCardViewModel> sectionCards, 
            IViewModelContextProvider viewModelContextProvider,
            Guid stageId) 
            : base("SectionStatusStepCard", viewModelContextProvider)
        {
            try
            {
                Step = step;
                StepName = GetStepName(viewModelContextProvider, Step.RenderStepType, stageId);
                SectionCards = new ObservableCollection<SectionCardViewModel>(sectionCards);
                ToggleSectionsCommand = ReactiveCommand.Create(ToggleSections);
            }
            catch (Exception ex)
            {
                var message = ex.Message;
            }
        }
        
        private void ToggleSections()
        {
            ShowSections = !ShowSections;
        }

        public override void Dispose()
        {
            foreach (var sectionCardViewModel in SectionCards)
            {
                sectionCardViewModel.Dispose();
            }
            SectionCards?.Clear();

            Step = null;

            ToggleSectionsCommand?.Dispose();
            ToggleSectionsCommand = null;

            base.Dispose();
        }
    }
}