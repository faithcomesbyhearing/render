using System.Reactive;

namespace Render.Kernel
{
    // The same as System.Reactive.Stubs, but public
    public static class Stubs
    {
        public static readonly Action<Unit> ActionNop = _ => { };

        public static readonly Action<Exception> ExceptionNop = _ => { };
    }
}