namespace Render.Components.UserProfileIcon
{
    public partial class UserProfileIcon
    {
        public UserProfileIcon()
        {
            InitializeComponent();
        }

        public static readonly BindableProperty UserIconProperty = BindableProperty.Create(
            nameof(UserIcon),
            typeof(ImageSource),
            typeof(UserProfileIcon),
            propertyChanged: ImagePropertyChanged);

        public ImageSource UserIcon
        {
            get => (ImageSource)GetValue(UserIconProperty);
            set => SetValue(UserIconProperty, value);
        }

        private static void ImagePropertyChanged(BindableObject bindable, object oldvalue, object newvalue)
        {
            var control = (UserProfileIcon)bindable;
            control.ImageButton.SetValue(ImageButton.SourceProperty, newvalue);
            var showImage = control.UserIcon != null;
            control.ImageButton.IsVisible = showImage;
            control.Label.IsVisible = !showImage;
        }
        
        public static readonly BindableProperty UserNameFirstLetterProperty = BindableProperty.Create(
            "",
            typeof(string),
            typeof(UserProfileIcon),
            propertyChanged: UsernameLabelPropertyChanged);

        public string UserNameFirstLetter
        {
            get => (string)GetValue(UserNameFirstLetterProperty);
            set => SetValue(UserNameFirstLetterProperty, value);
        }

        private static void UsernameLabelPropertyChanged(BindableObject bindable, object oldvalue, object newvalue)
        {
            var control = (UserProfileIcon)bindable;
            control.Label.Text = (string)newvalue;
            control.Label.IsVisible = control.UserIcon == null;
        }


        public static readonly BindableProperty DesiredWidthProperty = BindableProperty.Create(
            nameof(DesiredWidth),
            typeof(Double),
            typeof(UserProfileIcon),
            propertyChanged: DesiredWidthPropertyChanged);

        private static void DesiredWidthPropertyChanged(BindableObject bindable, object oldvalue, object newvalue)
        {
            var control = (UserProfileIcon)bindable;
            var width = (double)newvalue;
            var cornerRadius = width / 2;
            control.Stack.HeightRequest = width;
            control.Stack.WidthRequest = width;
            control.Frame.CornerRadius = (float)cornerRadius;
            control.FrameInner.CornerRadius = (float)cornerRadius;
        }

        public double DesiredWidth
        {
            get => (double)GetValue(DesiredWidthProperty);
            set => SetValue(DesiredWidthProperty, value);
        }
    }
}