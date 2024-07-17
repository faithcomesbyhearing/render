using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Sections;
using Render.Resources;

namespace Render.Pages.Settings.SectionStatus.Processes
{
    public class SectionCollectionAsStageCardViewModel : ViewModelBase
    {
        [Reactive]
        public string Glyph { get; private set; }
        public ReactiveCommand<Unit, Unit> ToggleStepsCommand;
        [Reactive] public bool ShowSections { get; set; }

        [Reactive] public bool HasSections { get; set; }
        public string Title { get; }
        public ObservableCollection<SectionCardViewModel> Sections { get; }
        
        public SectionCollectionAsStageCardViewModel(List<Section> unassignedSections, 
            Func<Guid, Task> sectionSelectCallback,
            Icon icon,
            string title,
            IViewModelContextProvider viewModelContextProvider) 
            : base("UnassignedSectionCollectionAsStageCard", viewModelContextProvider)
        {
            Glyph = IconExtensions.BuildFontImageSource(icon)?.Glyph;
            Title = title;
            ToggleStepsCommand = ReactiveCommand.Create(ToggleSections);
            Sections = new ObservableCollection<SectionCardViewModel>(unassignedSections
                .Select(s => new SectionCardViewModel(s, viewModelContextProvider, sectionSelectCallback)));
            var lastCard = Sections.LastOrDefault();
            if (lastCard != null)
            {
                lastCard.IsLastSectionCard = true;
            }
            
            HasSections = Sections.Any();
        }

        private void ToggleSections()
        {
            ShowSections = !ShowSections;
        }

        public override void Dispose()
        {
            foreach (var sectionCardViewModel in Sections)
            {
                sectionCardViewModel.Dispose();
            }
            Sections?.Clear();

            ToggleStepsCommand?.Dispose();
            ToggleStepsCommand = null;

            base.Dispose();
        }
    }
}