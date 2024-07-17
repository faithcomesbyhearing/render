namespace Render.Sequencer.Core.Utils.Extensions;

public static class DisposableExtensions
{
    public static IDisposable ToDisposables(this IDisposable disposable, List<IDisposable> disposables)
    {
        disposables.Add(disposable);

        return disposable;
    }
}