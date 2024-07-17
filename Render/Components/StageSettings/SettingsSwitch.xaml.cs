using System;

namespace Render.Components.StageSettings
{
    public partial class SettingsSwitch
    {
        #region IsToggledProperty

        public static readonly BindableProperty IsToggledProperty = BindableProperty.Create(
          propertyName: nameof(IsToggled),
          returnType: typeof(bool),
          declaringType: typeof(SettingsSwitch),
          defaultValue: default(bool),
          propertyChanged: IsToggledPropertyChanged);

        public bool IsToggled
        {
            get => (bool)GetValue(IsToggledProperty);
            set => SetValue(IsToggledProperty, value);
        }

        private static void IsToggledPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            Redraw(bindable as SettingsSwitch);
        }

        #endregion

        #region OnBackgroundColorProperty

        public static readonly BindableProperty OnBackgroundColorProperty = BindableProperty.Create(
             propertyName: nameof(OnBackgroundColor),
             returnType: typeof(Color),
             declaringType: typeof(SettingsSwitch),
             defaultValue: default(Color),
             propertyChanged: OnBackgroundColorPropertyChanged);

        public Color OnBackgroundColor
        {
            get => (Color)GetValue(OnBackgroundColorProperty);
            set => SetValue(OnBackgroundColorProperty, value);
        }

        private static void OnBackgroundColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            Redraw(bindable as SettingsSwitch);
        }

        #endregion

        #region OffBackgroundColorProperty

        public static readonly BindableProperty OffBackgroundColorProperty = BindableProperty.Create(
            propertyName: nameof(OffBackgroundColor),
            returnType: typeof(Color),
            declaringType: typeof(SettingsSwitch),
            defaultValue: default(Color),
            propertyChanged: OffBackgroundColorPropertyChanged);


        public Color OffBackgroundColor
        {
            get => (Color)GetValue(OffBackgroundColorProperty);
            set => SetValue(OffBackgroundColorProperty, value);
        }

        private static void OffBackgroundColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            Redraw(bindable as SettingsSwitch);
        }

        #endregion

        #region OnThumbColor

        public static readonly BindableProperty OnThumbColorProperty = BindableProperty.Create(
           propertyName: nameof(OnThumbColor),
           returnType: typeof(Color),
           declaringType: typeof(SettingsSwitch),
           defaultValue: default(Color),
           propertyChanged: OnThumbColorPropertyChanged);

        public Color OnThumbColor
        {
            get => (Color)GetValue(OnThumbColorProperty);
            set => SetValue(OnThumbColorProperty, value);
        }

        private static void OnThumbColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            Redraw(bindable as SettingsSwitch);
        }

        #endregion

        #region OffThumbColorProperty

        public static readonly BindableProperty OffThumbColorProperty = BindableProperty.Create(
            propertyName: nameof(OffThumbColor),
            returnType: typeof(Color),
            declaringType: typeof(SettingsSwitch),
            defaultValue: default(Color),
            propertyChanged: OffThumbColorPropertyChanged);

        public Color OffThumbColor
        {
            get => (Color)GetValue(OffThumbColorProperty);
            set => SetValue(OffThumbColorProperty, value);
        }

        private static void OffThumbColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            Redraw(bindable as SettingsSwitch);
        }

        #endregion

        public SettingsSwitch()
        {
            InitializeComponent();

            PropertyChanged += OnPropertyChangedHandler;
        }

        private void OnPropertyChangedHandler(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FlowDirection))
            {
                Redraw(sender as SettingsSwitch);
            }

            if (e.PropertyName == nameof(IsEnabled))
            {
                var customSwitch = sender as SettingsSwitch;

                customSwitch.LeftPanelOverlay.IsVisible = !customSwitch.IsEnabled;
                customSwitch.CenterPanelOverlay.IsVisible = !customSwitch.IsEnabled;
                customSwitch.RightPanelOverlay.IsVisible = !customSwitch.IsEnabled;
            }
        }

        private void Tapped(object sender, EventArgs e)
        {
            if (IsEnabled)
            {
                IsToggled = !IsToggled;
            }
        }

        private static void Redraw(SettingsSwitch switchControl)
        {
            if (switchControl.IsToggled)
            {
                switchControl.CustomToggle.HorizontalOptions = switchControl.FlowDirection == FlowDirection.LeftToRight
                    ? LayoutOptions.End
                    : LayoutOptions.Start;

                switchControl.LeftPanel.BackgroundColor = switchControl.OnBackgroundColor;
                switchControl.CenterPanel.BackgroundColor = switchControl.OnBackgroundColor;
                switchControl.RightPanel.BackgroundColor = switchControl.OnBackgroundColor;

                switchControl.CustomToggle.BackgroundColor = switchControl.OnThumbColor;
            }
            else
            {
                switchControl.CustomToggle.HorizontalOptions = switchControl.FlowDirection == FlowDirection.LeftToRight 
                    ? LayoutOptions.Start
                    : LayoutOptions.End;

                switchControl.LeftPanel.BackgroundColor = switchControl.OffBackgroundColor;
                switchControl.CenterPanel.BackgroundColor = switchControl.OffBackgroundColor;
                switchControl.RightPanel.BackgroundColor = switchControl.OffBackgroundColor;

                switchControl.CustomToggle.BackgroundColor = switchControl.OffThumbColor;
            }
        }
    }
}