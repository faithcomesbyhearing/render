using System.Runtime.CompilerServices;

namespace Render.Common;

public partial class LoadingScreen : ContentView
{
    private string _gearAnimationHandle;
    private Animation _gearAnimation;

    public LoadingScreen()
    {
        InitializeComponent();

        Unloaded += LoadingScreenUnloaded;
    }

    protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        if (propertyName == nameof(IsVisible))
        {
            ManageAnimation();
        }

        base.OnPropertyChanged(propertyName);
    }

    private void LoadingScreenUnloaded(object sender, EventArgs e)
    {
        DestroyAnimation();
    }

    private void ManageAnimation()
    {
        if (IsVisible)
        {
            DestroyAnimation();

            _gearAnimationHandle = Guid.NewGuid().ToString();
            _gearAnimation = new Animation
            {
                { 0, 1, new Animation(v => LargeGear.Rotation = v, 0, 360, Easing.Linear) },
                { 0, 1, new Animation(v => SmallGear.Rotation = v, 360, 0, Easing.Linear) },
            };

            _gearAnimation.Commit(this, _gearAnimationHandle, length: 3000, repeat: () => true);
        }
        else
        {
            DestroyAnimation();
        }
    }

    private void DestroyAnimation()
    {
        if (_gearAnimation is null)
        {
            return;
        }

        if (string.IsNullOrEmpty(_gearAnimationHandle) is false)
        {
            this.AbortAnimation(_gearAnimationHandle);
        }

        _gearAnimation.Dispose();
        _gearAnimation = null;
        _gearAnimationHandle = null;
    }
}