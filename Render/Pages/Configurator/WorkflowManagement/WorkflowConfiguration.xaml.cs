using ReactiveUI;

namespace Render.Pages.Configurator.WorkflowManagement;

public partial class WorkflowConfiguration
{
    private const int MinimalAllowedWidth = 1150;

    public WorkflowConfiguration()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection,
                v => v.TopLevelElement.FlowDirection));
            d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, v => v.TitleBar.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.ScrollerViewModel, v => v.MiniScrollerContainer.BindingContext));

            d(this.Bind(ViewModel, vm => vm.ScrollerViewModel.WidthRatio, v => v.StagesScrollView.WidthRatio));
            d(this.Bind(ViewModel, vm => vm.ScrollerViewModel.VisibleScreenWidth, v => v.StagesScrollView.Width));
            d(this.Bind(ViewModel, vm => vm.ScrollerViewModel.TotalWidth, v => v.StagesScrollView.TotalWidth));
            d(this.Bind(ViewModel, vm => vm.ScrollerViewModel.InputScrollX, v => v.StagesScrollView.InputScrollX));
            d(this.Bind(ViewModel, vm => vm.ScrollerViewModel.OutputScrollX, v => v.StagesScrollView.ScrollX));
            d(this.OneWayBind(ViewModel, vm => vm.ProceedButtonViewModel, v => v.ProceedButton.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.StagesTypes.Items, v => v.StagesCollectionView.ItemsSource));
            d(this.WhenAnyValue(x => x.ViewModel.StageCards)
                .Subscribe(x =>
                {
                    var source = BindableLayout.GetItemsSource(StagesStackLayout);
                    if (source == null)
                    {
                        BindableLayout.SetItemsSource(StagesStackLayout, x);
                    }

                }));

            d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.LoadingView.IsVisible));

            StagesScrollView.SizeChanged += OnStagesCardSizeChanged;
            StagesStackLayout.SizeChanged += OnStagesCardSizeChanged;
        });
    }
    
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        ChangeCardsHeightAndShowScroller();
    }

    private void OnStagesCardSizeChanged(object sender, EventArgs e) => ChangeCardsHeightAndShowScroller();
    
    private void ChangeCardsHeightAndShowScroller()
    {
        StagesStackLayout.HeightRequest = StagesScrollView.Height;
        var countOfStageCards = ViewModel?.StageCards.Count;
        var isSmallScreen = StagesScrollView.Width <= MinimalAllowedWidth;
        MiniScrollerContainer.SetValue(IsVisibleProperty, countOfStageCards > 3 || isSmallScreen);
    }
}