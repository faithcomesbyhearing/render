using Render.Models.Sections;
using Render.Models.Workflow;

namespace Render.Kernel.NavigationFactories
{
    public interface IStepViewModelDispatcher
    {
        Task<ViewModelBase> GetViewModelToNavigateTo(
            IViewModelContextProvider viewModelContextProvider,
            Step step, 
            Section section);
    }
}