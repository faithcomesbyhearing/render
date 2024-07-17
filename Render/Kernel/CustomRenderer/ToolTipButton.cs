namespace Render.Kernel.CustomRenderer;

public class ToolTipButton : Button
{
    
    public static readonly BindableProperty TipTextProperty =
        BindableProperty.Create(
            propertyName: "TextProperty",
            returnType: typeof(Label),
            declaringType: typeof(ToolTipButton),
            defaultValue: null);
    
    public static readonly BindableProperty IsToolTipOpenedProperty =
        BindableProperty.Create(
            propertyName: "IsToolTipOpened",
            returnType: typeof(bool),
            declaringType: typeof(ToolTipButton),
            defaultValue: false);


    public bool IsToolTipOpened
    {
        get => (bool)GetValue(IsToolTipOpenedProperty);
        set => SetValue(IsToolTipOpenedProperty, value);
    }
    
    public Label ToolTipText
    {
        get => (Label)GetValue(TipTextProperty);
        set => SetValue(TipTextProperty, value);
    }


    public ToolTipButton()
    {
        Clicked += (sender, args) =>
        {
            IsToolTipOpened = !IsToolTipOpened;
        };
    }
}