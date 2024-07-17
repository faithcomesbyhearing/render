using System.Collections.ObjectModel;
using System.Linq;
using Render.Kernel;

namespace Render.Pages.Settings.AudioExport.AllSectionsView
{
    public class AllSectionsViewViewModel : ViewModelBase
    {
        public readonly ReadOnlyObservableCollection<SectionToExport> Sections;
        
        public AllSectionsViewViewModel(IViewModelContextProvider viewModelContextProvider, ReadOnlyObservableCollection<SectionToExport> sections)
        : base("AllSectionsView", viewModelContextProvider)
        {
            var orderSections = sections.OrderBy(x => x.Section.Number).ToList();
            Sections = new ReadOnlyObservableCollection<SectionToExport>(new ObservableCollection<SectionToExport>(orderSections));
        }
    }
}