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

        [Reactive] 
        public bool ShowSections { get; set; }

        [Reactive] 
        public bool HasSections { get; set; }

        public string Title { get; }

        public ObservableCollection<SectionCardViewModel> Sections { get; }

        public ReactiveCommand<Unit, Unit> ToggleStepsCommand;

        public SectionCollectionAsStageCardViewModel(
            List<Section> unassignedSections, 
            Func<SectionCardViewModel, Task> sectionSelectCallback, 
            Action<SectionCardViewModel> selectSectionToExportCallback,
            Icon icon,
            string title,
            IViewModelContextProvider viewModelContextProvider) 
            : base("UnassignedSectionCollectionAsStageCard", viewModelContextProvider)
        {
            Glyph = IconExtensions.BuildFontImageSource(icon)?.Glyph;
            Title = title;
            ToggleStepsCommand = ReactiveCommand.Create(ToggleSections);

            var unassignedSectionCards = unassignedSections.Select(section => 
                new SectionCardViewModel(
                    section, 
                    viewModelContextProvider, 
                    sectionSelectCallback, 
                    selectSectionToExportCallback));
            Sections = new ObservableCollection<SectionCardViewModel>(unassignedSectionCards);

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