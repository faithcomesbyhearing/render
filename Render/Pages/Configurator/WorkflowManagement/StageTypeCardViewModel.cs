using Render.Kernel;
using Render.Models.Workflow;
using Render.Resources;

namespace Render.Pages.Configurator.WorkflowManagement;

public class StageTypeCardViewModel : ViewModelBase
{
    public string Name { get; set; }
    public string Glyph { get; set; }
    public StageTypes StageType { get; set; }

    public StageTypeCardViewModel(
        StageTypes stageType,
        IViewModelContextProvider viewModelContextProvider,
        Icon icon)
        : base(
            urlPathSegment: "StageTypeCard",
            viewModelContextProvider: viewModelContextProvider)
    {
        StageType = stageType;
        Name = Utilities.Utilities.GetStageNameFromResources(stageType);
        Glyph = IconExtensions.BuildFontImageSource(icon).Glyph;
    }
}