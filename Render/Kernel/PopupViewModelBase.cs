using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using ReactiveUI;
using Render.Kernel.CustomRenderer;

namespace Render.Kernel;

public class PopupViewModelBase<TResult> : ViewModelBase
{
    private CustomPopup _popup;
    private TaskCompletionSource<TResult> _taskCompletionSource;
    private Task<TResult> PopupClosedTask => _taskCompletionSource.Task;

    public PopupViewModelBase(string urlPathSegment, 
        IViewModelContextProvider viewModelContextProvider, 
        IScreen screen = null) 
        : base(urlPathSegment, viewModelContextProvider, screen)
    {
    }

    public async Task<TResult> ShowPopupAsync()
    {
        _taskCompletionSource = new TaskCompletionSource<TResult>();

        var view = (View)ViewLocator.Current.ResolveView(this);

        if (view is null)
        {
            throw new ArgumentException($"View can not be found for {this.GetType()} View Model");
        }

        view.BindingContext = this;
        var page = Application.Current?.MainPage ?? throw new NullReferenceException();
        _popup = new CustomPopup()
        {
            Content = view,
            Color = Colors.Transparent,
            CanBeDismissedByTappingOutsideOfPopup = false,
            HorizontalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Fill,
            VerticalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Fill
        };

        _popup.Closed += OnPopupClosed;
        view.Loaded += OnPopupContentLoaded;

        await page.ShowPopupAsync(_popup);

        return await PopupClosedTask;
    }

    public void ClosePopup(TResult result)
    {
        _popup?.Close();
        _taskCompletionSource?.SetResult(result);
    }

    protected virtual void OnPopupContentLoaded(object sender, EventArgs e)
    {
    }

    protected virtual void OnPopupClosed(object sender, PopupClosedEventArgs e)
    {
        _popup.Closed -= OnPopupClosed;
        _popup = null;
    }
}
