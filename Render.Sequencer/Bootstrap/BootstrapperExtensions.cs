using Render.Sequencer.Core.Controls;
using Render.Sequencer.Platforms;
using Render.Sequencer.Views.Flags.Base;

namespace Render.Sequencer.Bootstrap
{
    public static class BootstrapperExtensions
    {
        public static MauiAppBuilder UseSequencer(this MauiAppBuilder builder)
        {
            return builder.AddHandlers();
        }

        private static MauiAppBuilder AddHandlers(this MauiAppBuilder builder)
        {
            return builder.ConfigureMauiHandlers(AddHandlers);

            void AddHandlers(IMauiHandlersCollection handlers)
            {
                handlers.AddHandler(typeof(Scrubber), typeof(ScrubberHandler));
                handlers.AddHandler(typeof(BaseFlagView), typeof(FlagViewHandler));
            }
        }
    }
}