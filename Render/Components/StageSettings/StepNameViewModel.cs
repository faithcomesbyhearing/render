using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Workflow;

namespace Render.Components.StageSettings;

public class StepNameViewModel : ReactiveObject
{
    private readonly Step _step;
    private readonly string _initialStepName;

    [Reactive] public string StepName { get; set; }

    public StepNameViewModel(Step step)
    {
        _step = step;
        _initialStepName = step.GetName();
        StepName = _initialStepName;
    }

    public void UpdateEntity()
    {
        var initialStepName = _initialStepName.Trim();
        var stepName = StepName.Trim();

        if (string.Equals(initialStepName, stepName, StringComparison.OrdinalIgnoreCase) is false)
        {
            _step.CustomName = StepName;
        }
    }
}