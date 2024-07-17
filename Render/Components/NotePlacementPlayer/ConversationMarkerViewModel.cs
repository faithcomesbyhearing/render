using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Sections;

namespace Render.Components.NotePlacementPlayer
{
    public enum FlagState
    {
        Optional,
        Required,
        Viewed
    }

    [Obsolete("ConversationViewModel should replace ConversationMarkerViewModel")]
    public class ConversationMarkerViewModel
    {
        public double TimeMarker { get; }

        [Reactive]
        public Conversation Conversation { get; private set; }

        [Reactive]
        public FlagState FlagState { get; set; }

        [Reactive]
        public double Width { get; set; }

        [Reactive]
        public bool IsUpsideDown { get; set; }

        public bool IsPeekaboo { get; }
        public bool IsBeginningPeekaboo { get; }
        public bool IsEndPeekaboo { get; }

        public ConversationMarkerViewModel(
            Conversation conversation,
            bool isNotMiniScrubber,
            Guid markerId,
            double totalTime,
            IViewModelContextProvider viewModelContextProvider,
            double startTimeOffset = 0,
            bool needToSetRequiredFlagStateInsteadOptional = false,
            double overrideTimeMarker = -1d)
        { }

        public void Dispose()
        {

        }
    }
}