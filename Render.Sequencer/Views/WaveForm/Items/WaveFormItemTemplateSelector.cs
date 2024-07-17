using Render.Sequencer.Core.Utils.Errors;
using Render.Sequencer.Views.WaveForm.Items.Combining;
using Render.Sequencer.Views.WaveForm.Items.Editable;

namespace Render.Sequencer.Views.WaveForm.Items;

internal class WaveFormItemTemplateSelector : DataTemplateSelector
{
    private DataTemplate _waveFormItemTemplate = new DataTemplate(() => new WaveFormItemView());
    private DataTemplate _combiningWaveFormItemTemplate = new DataTemplate(() => new CombiningWaveFormItemView());
    private DataTemplate _combinableWaveFormItemTemplate = new DataTemplate(() => new CombinableWaveFormItemView());
    private DataTemplate _editableWaveFormItemTemplate = new DataTemplate(() => new EditableWaveFormItemView());

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        return item switch
        {
            WaveFormItemViewModel => _waveFormItemTemplate,
            CombiningWaveFormItemViewModel => _combiningWaveFormItemTemplate,
            CombinableWaveFormItemViewModel => _combinableWaveFormItemTemplate,
            EditableWaveFormItemViewModel => _editableWaveFormItemTemplate,
            _ => throw new InvalidNavigationException(ErrorMessages.InvalidWaveFormItemType)
        };
    }
}