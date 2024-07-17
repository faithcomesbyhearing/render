using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;

namespace Render.Kernel.WrappersAndExtensions
{
    public static class ReactiveCommandExtensions
    {
        public static ReactiveCommand<Unit, TResult> AddCondition<TResult>(this ReactiveCommand<Unit, TResult> command, Func<Task<bool>> commandCondition)
        {
            var innerCommand = command;

            return ReactiveCommand.CreateFromTask(async () =>
            {
                if (commandCondition != null && !await commandCondition())
                {
                    return default;
                }

                return await innerCommand.Execute();
            });
        }

        /// <summary>
        /// Add ReactiveCommand to observable pipeline to prevent multiple command execution.
        /// Executes command through the InvokeCommand extension which respects command executability state.
        /// Add throttle for tap\click events to exclude accedential clicks.
        /// See details here:
        /// https://github.com/reactiveui/ReactiveUI/issues/3022
        /// </summary>
        public static IDisposable BindCommandCustom<TView, TParam, TResult>(
            this TView view,
            TapGestureRecognizer tapGestureRecognizer,
            Expression<Func<TView, ReactiveCommandBase<TParam, TResult>>> viewModelProperty,
            Func<EventPattern<object>, TParam> eventArgsToCommandParamFunc=null,
            double throttleMs=500)
            where TView : class, IViewFor
        {

            return Observable
                .FromEventPattern(tapGestureRecognizer, nameof(TapGestureRecognizer.Tapped))
                .ThrottleFirst(throttleMs)
                .InvokeCommand(view, viewModelProperty, eventArgsToCommandParamFunc);
        }

        /// <summary>
        /// <inheritdoc cref="BindCommandCustom"/>
        /// </summary>
        public static IDisposable BindCommandCustom<TView, TParam, TResult>(
            this TView view,
            Button button,
            Expression<Func<TView, ReactiveCommandBase<TParam, TResult>>> viewModelProperty,
            Func<EventPattern<object>, TParam> eventArgsToCommandParamFunc=null,
            double throttleMs=500)
            where TView : class, IViewFor
        {

            return Observable
                .FromEventPattern(button, nameof(Button.Clicked))
                .ThrottleFirst(throttleMs)
                .InvokeCommand(view, viewModelProperty, eventArgsToCommandParamFunc);
        }

        /// <summary>
        /// RxNET extensions don't have actual throttle operator.
        /// Default Throttle extension behaves like debounce.
        /// See details here:
        /// https://github.com/dotnet/reactive/issues/395
        /// </summary>
        private static IObservable<EventPattern<object>> ThrottleFirst(
            this IObservable<EventPattern<object>> eventObservable,
            double throttleMs=500)
        {
            return eventObservable
                .Take(1)
                .Concat(Observable
                    .Empty<EventPattern<object>>()
                    .Delay(TimeSpan.FromMilliseconds(throttleMs), RxApp.MainThreadScheduler))
                .Repeat();
        }

        private static IDisposable InvokeCommand<TView, TParam, TResult>(
            this IObservable<EventPattern<object>> eventObservable, 
            TView view, 
            Expression<Func<TView, ReactiveCommandBase<TParam, TResult>>> viewModelProperty,
            Func<EventPattern<object>, TParam> eventArgsToCommandParamFunc=null)
            where TView : class, IViewFor
        {
            return eventObservable
                .Select(eventPattern => eventArgsToCommandParamFunc is null ? default : eventArgsToCommandParamFunc(eventPattern))
                .InvokeCommand(view, viewModelProperty);
        }
    }
}