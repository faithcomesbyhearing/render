using System.Reactive;
using ReactiveUI;

namespace Render.Kernel
{
    public interface IActionViewModelBase: IViewModelBase, IReactiveObject
    {
        ActionState ActionState { get; set; }

        string UrlPathSegment { get; }

        ReactiveCommand<Unit, IRoutableViewModel> NavigateBackCommand { get; set; }
    }
}