namespace Render.Kernel.CustomRenderer
{
    public class Panel : Frame
    {
        public static readonly BindableProperty BorderRadiusProperty = BindableProperty.Create(
            propertyName: nameof(BorderRadius),
            returnType: typeof(CornerRadius),
            declaringType: typeof(Panel),
            defaultValue: default(CornerRadius));
        
        public static readonly BindableProperty BorderThicknessProperty = BindableProperty.Create(
            propertyName: nameof(BorderThickness), 
            returnType: typeof(Thickness), 
            declaringType: typeof(Panel), 
            defaultValue: default(Thickness));

        public CornerRadius BorderRadius
        {
            get => (CornerRadius)GetValue(BorderRadiusProperty);
            set => SetValue(BorderRadiusProperty, BorderRadius);
        }

        public Thickness BorderThickness
        {
            get => (Thickness)GetValue(BorderThicknessProperty);
            set => SetValue(BorderThicknessProperty, BorderThickness);
        }
    }
}