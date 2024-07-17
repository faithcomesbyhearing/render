using ReactiveUI;

namespace Render.Pages.AppStart.SplashScreen;

public partial class SplashScreen : IDisposable
{
    public SplashScreen()
    {
        InitializeComponent();

        this.WhenActivated(_ =>
        {
            ViewModel?.NavigateToLoginAsync();
        });
    }

    public void Dispose()
    {
        ViewModel?.Dispose();
        ViewModel = null;
    }
}
