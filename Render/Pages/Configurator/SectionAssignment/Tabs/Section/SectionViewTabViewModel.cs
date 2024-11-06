using Render.Kernel;
using Render.Pages.Configurator.SectionAssignment.Manager;

namespace Render.Pages.Configurator.SectionAssignment.Tabs.Section;

public class SectionViewTabViewModel : ViewModelBase
{
    public SectionCollectionsManager Manager { get; private set; }

    public SectionViewTabViewModel(
        IViewModelContextProvider viewModelContextProvider,
        SectionCollectionsManager manager)
        : base("SectionViewTabView", viewModelContextProvider)
    {
        Manager = manager;
    }

    public override void Dispose()
    {
        Manager = null;

        base.Dispose();
    }
}