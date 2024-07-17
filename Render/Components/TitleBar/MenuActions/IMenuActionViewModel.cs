using System.Reactive;
using ReactiveUI;

namespace Render.Components.TitleBar.MenuActions
{
    public interface IMenuActionViewModel : IDisposable
    {
        string Title { get; }

        string Glyph { get; }

        bool IsActionExecuting { get; set; }

        bool CanActionExecute { get; set; }

        ReactiveCommand<Unit, IRoutableViewModel> Command { get; }

        void SetCommandCondition(Func<Task<bool>> condition);
    }
}