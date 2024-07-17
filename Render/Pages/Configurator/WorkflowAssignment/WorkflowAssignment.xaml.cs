using ReactiveUI;

namespace Render.Pages.Configurator.WorkflowAssignment;

public partial class WorkflowAssignment 
{
    public WorkflowAssignment()
    {
        InitializeComponent();
        
        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, 
                v => v.TopLevelElement.FlowDirection));
            d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, 
                v => v.TitleBar.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.ProceedButtonViewModel,
                v => v.ProceedButton.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.ScrollerViewModel, v => v.MiniScrollerContainer.BindingContext));

            d(this.Bind(ViewModel, vm => vm.ScrollerViewModel.WidthRatio, 
                v => v.StagesScrollView.WidthRatio));
            d(this.Bind(ViewModel, vm => vm.ScrollerViewModel.VisibleScreenWidth,
                v => v.StagesScrollView.Width));
            d(this.Bind(ViewModel, vm => vm.ScrollerViewModel.TotalWidth, 
                v => v.StagesScrollView.TotalWidth));
            d(this.Bind(ViewModel, vm => vm.ScrollerViewModel.InputScrollX, 
                v => v.StagesScrollView.InputScrollX));
            d(this.Bind(ViewModel, vm => vm.ScrollerViewModel.OutputScrollX, 
                v => v.StagesScrollView.ScrollX));
            
            d(this.WhenAnyValue(x => x.ViewModel.Users.Items)
                .Subscribe(x =>
                {
                    var source = BindableLayout.GetItemsSource(UserAssignmentCollection);
                    if (source == null)
                    {
                        BindableLayout.SetItemsSource(UserAssignmentCollection, x);
                    }
                }));
            
            d(this.WhenAnyValue(x => x.ViewModel.StageColumns)
                .Subscribe(x =>
                {
                    var source = BindableLayout.GetItemsSource(StageColumnCollection);
                    if (source == null)
                    {
                        BindableLayout.SetItemsSource(StageColumnCollection, x);
                    }
                }));
                
            d(this.OneWayBind(ViewModel, vm => vm.IsLoading, 
                v => v.LoadingView.IsVisible));

            StagesScrollView.SizeChanged += OnStagesCardSizeChanged;
            StageColumnCollection.SizeChanged += OnStagesCardSizeChanged;
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
        StageColumnCollection.HeightRequest = StagesScrollView.Height;
        var countOfStageCards = ViewModel?.StageColumns.Count;
        var isSmallScreen = StagesScrollView.Width <= 1150;
        MiniScrollerContainer.SetValue(IsVisibleProperty, countOfStageCards > 3 || isSmallScreen);
    }
}