using System.Reactive.Linq;
using ReactiveUI;
using Render.Extensions;
using Render.Kernel.TouchActions;
using Render.Pages.AppStart.Home.NavigationIcons;

namespace Render.Pages.AppStart.Home.NavigationPanels;

public partial class NavigationPane
{
    private double _widthRatio;
    private double _heightRatio;
    private double scale = 1.0;
    private double scrollHeight;
    private double heightRequest = 200;
    private double widthRequest = 225;
    private DisplayOrientation orientation = DisplayOrientation.Unknown;

    public NavigationPane()
    {
        InitializeComponent();

        DisposableBindings = this.WhenActivated(d =>
        {
            d(this.WhenAnyValue(x => x.ViewModel.NavigationIcons.Items)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    BindableLayout.SetItemsSource(IconCollection, x);
                    BindableLayout.SetItemsSource(MiniIconCollection, x);
                }));
            d(this.WhenAnyValue(x => x.ViewModel.ShowMiniScrollBar)
                .Subscribe(x => { MiniScroller.SetValue(IsVisibleProperty, x); }));
            d(this.WhenAnyValue(x => x.MiniIconCollection.Width)
                .Subscribe(_ => { OnScreenSizeChanged(null, EventArgs.Empty); }));
        });

        SizeChanged += OnScreenSizeChanged;
        IconCollection.SizeChanged += OnSizeChanged;
        NavigationScrollView.Scrolled += ScrollViewOnScrolled;
        NavigationScrollView.Resources.Add("NavIconHeight", heightRequest);
        NavigationScrollView.Resources.Add("NavIconWidth", widthRequest);
    }

    private void OnScreenSizeChanged(object sender, EventArgs e)
    {
        if (DeviceDisplay.MainDisplayInfo.Orientation != orientation)
        {
            orientation = DeviceDisplay.MainDisplayInfo.Orientation;
            scale = 1.0;
            scrollHeight = default;
            heightRequest = 200;
            widthRequest = 225;
        }

        OnSizeChanged(sender, e);
    }

    private void OnSizeChanged(object sender, EventArgs e)
    {
        if (NavigationScrollView.Height > scrollHeight)
        {
            scrollHeight = NavigationScrollView.Height;
        }

        var paneThickness = new Thickness(25);
        var scrollerThickness = new Thickness(25);

        SetWidthRatio();

        NavPaneStack.SetValue(MarginProperty, paneThickness);
        MiniScroller.SetValue(MarginProperty, scrollerThickness);
    }

    private void SetWidthRatio()
    {
        if (IconCollection.Width <= 0) return;
        var onScreenRatio = NavigationScrollView.Width / IconCollection.Width;
        if (onScreenRatio > 1.0)
        {
            onScreenRatio = 1.0;
        }

        if (MiniIconCollection.Width <= 0)
        {
            VisibleFrame.SetValue(HeightRequestProperty, 50);
            return;
        }

        _widthRatio = MiniIconCollection.Width / IconCollection.Width;
        var frameWidth = onScreenRatio * MiniIconCollection.Width;

        VisibleFrame.SetValue(WidthRequestProperty, Math.Ceiling(frameWidth));
        VisibleFrame.SetValue(HeightRequestProperty, 36);
    }

    private async void MiniScroller_OnTouchAction(object sender, TouchActionEventArgs args)
    {
        if (!args.IsInContact || _widthRatio == 0.0)
            return;
        var position = args.Location.X / _widthRatio;
        var halfWidth = VisibleFrame.Width / 2 / _widthRatio;
        await NavigationScrollView.ScrollToAsync(Math.Max(0, position - halfWidth), 0, false);
    }

    private void ScrollViewOnScrolled(object sender, ScrolledEventArgs e)
    {
        if (ViewModel?.FlowDirection == FlowDirection.LeftToRight)
        {
            VisibleFrame.SetValue(TranslationXProperty, e.ScrollX * _widthRatio);
        }
        else
        {
            VisibleFrame.SetValue(TranslationXProperty,
                NavigationScrollView.ScrollX / (NavigationScrollView.Width / VisibleFrame.Width));
        }
    }

    protected override void Dispose(bool disposing)
    {
        ViewModel?.Dispose();

        IconCollection.Children
            .Cast<NavigationIcon>()
            .ForEach(icon => icon.Dispose());

        IconCollection.Children.Clear();

        MiniIconCollection.Children
            .Cast<MiniNavigationIcon>()
            .ForEach(icon => icon.Dispose());

        MiniIconCollection.Children.Clear();

        BindableLayout.SetItemsSource(IconCollection, null);
        BindableLayout.SetItemsSource(MiniIconCollection, null);

        base.Dispose(disposing);
    }
}