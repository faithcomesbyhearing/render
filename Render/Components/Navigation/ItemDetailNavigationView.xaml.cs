using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;
using Render.Resources;

namespace Render.Components.Navigation;

public partial class ItemDetailNavigationView
{
    public ItemDetailNavigationView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            d(this.BindCommandCustom(PreviousGesture, v => v.ViewModel.MoveToPreviousItemCommand));
            d(this.BindCommandCustom(NextGesture, v => v.ViewModel.MoveToNextItemCommand));

            d(this.WhenAnyValue(x => x.ViewModel.HasNextItem,
                    x => x.ViewModel.HasNextRequiredItem)
                .Subscribe(((bool hasNext, bool hasNextRequired) navigation) =>
                {
                    SetPickerState(NextPicker,
                        navigation.hasNext,
                        navigation.hasNextRequired);
                }));

            d(this.WhenAnyValue(x => x.ViewModel.HasPreviousItem,
                    x => x.ViewModel.HasPreviousRequiredItem)
                .Subscribe(((bool hasPrevious, bool hasPreviousRequired) navigation) =>
                {
                    SetPickerState(PreviousPicker,
                        navigation.hasPrevious,
                        navigation.hasPreviousRequired);
                }));
        });
    }

    private void SetPickerState(Label picker, bool enabled, bool required)
    {
        picker.SetValue(OpacityProperty, enabled ? 1 : 0.3);
        picker.SetValue(IsEnabledProperty, enabled);

        var color = ResourceExtensions.GetColor(required && enabled
            ? "Required"
            : "Option");

        picker.SetValue(Label.TextColorProperty, color);
    }
}