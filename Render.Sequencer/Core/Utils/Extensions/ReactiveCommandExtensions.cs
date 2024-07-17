using ReactiveUI;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace Render.Sequencer.Core.Utils.Extensions;

internal static class ReactiveCommandExtensions
{
    public static IObservable<TResult?> SafeExecute<TParam, TResult>(this ReactiveCommandBase<TParam, TResult>? command, TParam? param=default)
    {
        if (command is not null)
        {
            if (param is not null)
            {
                return command.Execute(param);
            }

            return command.Execute();
        }

        return Task.FromResult<TResult?>(default).ToObservable();
    }
}