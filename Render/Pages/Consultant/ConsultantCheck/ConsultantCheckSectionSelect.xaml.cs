using ReactiveUI;
using Render.Resources;

namespace Render.Pages.Consultant.ConsultantCheck;

public partial class ConsultantCheckSectionSelect
    {
        private readonly Color _selectedBackgroundColor = ResourceExtensions.GetColor("Option");
        private readonly Color _unSelectedBackgroundColor = ResourceExtensions.GetColor("Transparent");
        private readonly Color _selectedTextColor = ResourceExtensions.GetColor("SecondaryText");
        private readonly Color _unselectedTextColor = ResourceExtensions.GetColor("Option");
        
        public ConsultantCheckSectionSelect()
        {
            InitializeComponent();
            
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, 
                    v => v.TopLevelElement.FlowDirection));
                d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, 
                    v => v.TitleBar.BindingContext));
                
                d(this.BindCommand(ViewModel, vm => vm.SelectReviewedCommand, 
                    v => v.ReviewedButton));
                d(this.BindCommand(ViewModel, vm => vm.SelectUnreviewedCommand, 
                    v => v.UnreviewedButton));
                
                d(this.OneWayBind(ViewModel, vm => vm.IsLoading, 
                    v => v.LoadingView.IsVisible));
                
                d(this.WhenAnyValue(x => x.ViewModel.SectionsToCheck.Items)
                    .Subscribe(x =>
                    {
                        var source = BindableLayout.GetItemsSource(UnreviewedCardCollection);
                        if (source == null)
                        {
                            BindableLayout.SetItemsSource(UnreviewedCardCollection, x);
                        }
                    }));
                
                d(this.WhenAnyValue(x => x.ViewModel.CheckedSections.Items)
                    .Subscribe(x =>
                    {
                        var source = BindableLayout.GetItemsSource(ReviewedCardCollection);
                        if (source == null)
                        {
                            BindableLayout.SetItemsSource(ReviewedCardCollection, x);
                        }
                    }));

                d(this.WhenAnyValue(x => x.ViewModel.ReviewedSelected)
                    .Subscribe(x =>
                    {
                        NoReviewedSections.IsVisible = false;
                        NoUnreviewedSections.IsVisible = false;
                        if (x)
                        {
                            UnreviewedFrame.SetValue(BackgroundColorProperty, _unSelectedBackgroundColor);
                            UnreviewedIcon.TextColor = _unselectedTextColor;
                            UnreviewedText.TextColor = _unselectedTextColor;
                            ReviewedFrame.SetValue(BackgroundColorProperty, _selectedBackgroundColor);
                            ReviewedIcon.TextColor = _selectedTextColor;
                            ReviewedText.TextColor = _selectedTextColor;
                            UnreviewedCardCollection.IsVisible = false;
                            ReviewedCardCollection.IsVisible = true;

                            if (ViewModel != null && !ViewModel.HasCheckedSections)
                            {
                                NoReviewedSections.IsVisible = true;
                            }
                        }
                        else
                        {
                            UnreviewedFrame.SetValue(BackgroundColorProperty, _selectedBackgroundColor);
                            UnreviewedIcon.TextColor = _selectedTextColor;
                            UnreviewedText.TextColor = _selectedTextColor;
                            ReviewedFrame.SetValue(BackgroundColorProperty, _unSelectedBackgroundColor);
                            ReviewedIcon.TextColor = _unselectedTextColor;
                            ReviewedText.TextColor = _unselectedTextColor;
                            UnreviewedCardCollection.IsVisible = true;
                            ReviewedCardCollection.IsVisible = false;
                            
                            if (ViewModel != null && !ViewModel.HasUncheckedSections)
                            {
                                NoUnreviewedSections.IsVisible = true;
                            }
                        }
                    }));
            });
        }
    }