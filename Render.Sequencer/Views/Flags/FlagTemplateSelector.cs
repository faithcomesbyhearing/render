using Render.Sequencer.Core.Utils.Errors;

namespace Render.Sequencer.Views.Flags;

internal class FlagTemplateSelector : DataTemplateSelector
{
    private DataTemplate _noteFlagTemplate = new DataTemplate(() => new NoteFlagView());
    private DataTemplate _markerFlagTemplate = new DataTemplate(() => new MarkerFlagView());

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        return item switch
        {
            NoteFlagViewModel => _noteFlagTemplate,
            MarkerFlagViewModel => _markerFlagTemplate,
            _ => throw new InvalidNavigationException(ErrorMessages.InvalidFlagType)
        };
    }
}