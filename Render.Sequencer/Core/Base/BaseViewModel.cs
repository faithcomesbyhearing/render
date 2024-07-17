using ReactiveUI;

namespace Render.Sequencer.Core.Base;

public abstract class BaseViewModel : ReactiveObject, IDisposable
{
    protected readonly List<IDisposable> Disposables = new();

    public virtual void Dispose()
    {
        foreach (IDisposable disposable in Disposables)
        {
            disposable.Dispose();
        }
    }
}
