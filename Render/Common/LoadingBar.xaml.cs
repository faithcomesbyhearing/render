using Render.Resources;
using Render.Resources.Styles;

namespace Render.Common;

public partial class LoadingBar : ContentView
{
    private Color _color;
    private Animation _animation;

    public LoadingBar()
    {
        InitializeComponent();

        _color = ((ColorReference)ResourceExtensions.GetResourceValue("LoginPageLoadingCircleBackground")).Color;

        Loaded += LoadingBarLoaded;
        Unloaded += LoadingBarUnloaded;
    }

    private void LoadingBarLoaded(object sender, EventArgs e)
    {
        _animation = new Animation
        {
            {0,0.3, new Animation(v => Circle1.BackgroundColor = _color.MultiplyAlpha((float)v), 0, 1, Easing.Linear)},
            {0.3,0.6, new Animation(v => Circle1.BackgroundColor = _color.MultiplyAlpha((float)v), 1, 0, Easing.Linear)},
            {0.1,0.4, new Animation(v => Circle2.BackgroundColor = _color.MultiplyAlpha((float)v), 0, 1, Easing.Linear)},
            {0.4,0.7, new Animation(v => Circle2.BackgroundColor = _color.MultiplyAlpha((float)v), 1, 0, Easing.Linear)},
            {0.2,0.5, new Animation(v => Circle3.BackgroundColor = _color.MultiplyAlpha((float)v), 0, 1, Easing.Linear)},
            {0.5,0.8, new Animation(v => Circle3.BackgroundColor = _color.MultiplyAlpha((float)v), 1, 0, Easing.Linear)},
            {0.3,0.6, new Animation(v => Circle4.BackgroundColor = _color.MultiplyAlpha((float)v), 0, 1, Easing.Linear)},
            {0.6,0.9, new Animation(v => Circle4.BackgroundColor = _color.MultiplyAlpha((float)v), 1, 0, Easing.Linear)}
        };
        _animation.Commit(this, nameof(_animation), length: 3500, repeat: () => true);
    }

    private void LoadingBarUnloaded(object sender, EventArgs e)
    {
        this.AbortAnimation(nameof(_animation));

        _animation?.Dispose();
        _animation = null;
    }
}