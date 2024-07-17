using Render.Resources;

namespace Render.Components.SubTitle
{
    public partial class SubTitle
    {
        public static readonly BindableProperty IconTypeProperty = BindableProperty.Create(
            nameof(IconType),
            typeof(Icon),
            typeof(SubTitle));

        public Icon IconType
        {
            get => (Icon)GetValue(IconTypeProperty);
            set => SetValue(IconTypeProperty, value);
        }

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(SubTitle));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public SubTitle()
        {
            InitializeComponent();
        }

        protected override void OnPropertyChanged(string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == IconTypeProperty.PropertyName)
            {
                var icon = ResourceExtensions.GetResourceValue(IconType.ToString());

                IconLabel.SetValue(Label.TextProperty, icon);
            }

            if (propertyName == TextProperty.PropertyName)
            {
                TextLabel.SetValue(Label.TextProperty, Text);
            }
        }
    }
}