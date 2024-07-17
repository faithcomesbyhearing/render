using System.Windows.Input;

namespace Render.Sequencer.Views.Flags.Base;

public class BaseFlagView : ContentView
{
    public static readonly BindableProperty PositionXProperty = BindableProperty.Create(
        propertyName: nameof(PositionX),
        returnType: typeof(double),
        declaringType: typeof(BaseFlagView),
        defaultValue: 0d,
        propertyChanged: PositionXPropertyChanged);

    private static void PositionXPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is BaseFlagView flagView && newValue is double position) 
        {
            flagView.UpdatePositionX(position);
        }
    }

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        propertyName: nameof(Command),
        returnType: typeof(ICommand),
        declaringType: typeof(BaseFlagView),
        defaultValue: null);

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        propertyName: nameof(CommandParameter),
        returnType: typeof(object),
        declaringType: typeof(BaseFlagView),
        defaultValue: null);

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public double PositionX
    {
        get => (double)GetValue(PositionXProperty);
        set => SetValue(PositionXProperty, value);
    }

    public virtual string ViewTag
    {
        get => FlagTags.CommonFlag;
    }

    private void UpdatePositionX(double position)
    {
        if (Width <= 0)
        {
            return;
        }

        TranslationX = position - 0.5 * Width;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        UpdatePositionX(PositionX);
    }

    public virtual void SendTapped()
    {
        if (IsEnabled && Command?.CanExecute(CommandParameter) is true)
        {
            Command?.Execute(CommandParameter);
        }
    }
}
