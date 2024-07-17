using Render.Kernel;
using Render.Models.Workflow;
using Render.Resources;

namespace Render.Pages.Configurator.WorkflowManagement;

public class StageTypeCardViewModel : ViewModelBase
{
    public string Name { get; set; }
    public string Glyph { get; set; }
    public StageTypes StageType { get; set; }

    public StageTypeCardViewModel(StageTypes stageType, IViewModelContextProvider viewModelContextProvider, Icon icon)
        : base("StageTypeCard", viewModelContextProvider)
    {
        StageType = stageType;
        Name = Utilities.StageTypes.ToString(StageType);
        Glyph = IconExtensions.BuildFontImageSource(icon).Glyph;
    }
}