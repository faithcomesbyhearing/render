namespace Render.Components.ProgressBar
{
    public partial class RenderProgress
    {
        public RenderProgress()
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// The current value of the progress, between 0 and 1. 
        /// </summary>
        public static readonly BindableProperty ProgressPercentProperty = BindableProperty.Create(
            nameof(ProgressPercent),
            typeof(double),
            typeof(RenderProgress),
            propertyChanged: ProgressPropertyChanged);

        public double ProgressPercent
        {
            get => (double)GetValue(ProgressPercentProperty);
            set => SetValue(ProgressPercentProperty, value);
        }

        private static void ProgressPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (RenderProgress)bindable;
            var value = (double)newValue;
            control.ProgressBar.SetValue(AbsoluteLayout.LayoutBoundsProperty, new Rect(0,0,value, 1));
        }
        
        /// <summary>
        /// The background color of the non-filled in portion of the progress bar
        /// </summary>
        public static readonly BindableProperty ProgressBarBackgroundColorProperty = BindableProperty.Create(
            nameof(ProgressBarBackgroundColor),
            typeof(Color),
            typeof(RenderProgress),
            propertyChanged: ProgressBarBackgroundColorPropertyChanged);

        public Color ProgressBarBackgroundColor
        {
            get => (Color)GetValue(ProgressBarBackgroundColorProperty);
            set => SetValue(ProgressBarBackgroundColorProperty, value);
        }
        
        private static void ProgressBarBackgroundColorPropertyChanged(BindableObject bindable, object oldValue,
            object newValue)
        {
            var control = (RenderProgress)bindable;
            var value = (Color)newValue;
            control.ProgressContainer.SetValue(BackgroundColorProperty, value);
        }
        
        /// <summary>
        /// The color of the filled-in portion of the progress bar
        /// </summary>
        public static readonly BindableProperty ProgressBarColorProperty = BindableProperty.Create(
            nameof(ProgressBarColor),
            typeof(Color),
            typeof(RenderProgress),
            propertyChanged: ProgressColorPropertyChanged);

        public Color ProgressBarColor
        {
            get => (Color)GetValue(ProgressBarColorProperty);
            set => SetValue(ProgressBarColorProperty, value);
        }

        private static void ProgressColorPropertyChanged(BindableObject bindable, object oldValue,
            object newValue)
        {
            var control = (RenderProgress)bindable;
            var value = (Color)newValue;
            control.ProgressBar.SetValue(BackgroundColorProperty, value);
        }
    }
}