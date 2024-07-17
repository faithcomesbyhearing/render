namespace Render.Sequencer.Core.Utils.Extensions;

internal static class VisualElementExtensions
{
    internal static void AddBehavior<TBehavior>(this VisualElement visualElement, TBehavior behavior)
        where TBehavior : Behavior
    {
        visualElement.Behaviors.Add(behavior);
    }

    internal static TBehavior? GetBehavior<TBehavior>(this VisualElement visualElement)
        where TBehavior : Behavior
    {
        return visualElement.Behaviors.FirstOrDefault(behavior => behavior is TBehavior) as TBehavior;
    }

    internal static void RemoveBehavior<TBehavior>(this VisualElement visualElement, TBehavior? behavior)
        where TBehavior : Behavior
    {
        if (behavior is not null)
        {
            visualElement.Behaviors.Remove(behavior);
        }
    }
}