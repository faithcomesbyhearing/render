using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;
using Render.Resources;

namespace Render.Components.TitleBar;

public partial class TitleBar
{
    public const double Size = 75;
    public const double Spacing = 20;
    public const double IconWidth = 60;
    
    public TitleBar()
    {
        InitializeComponent();
        
        DisposableBindings = this.WhenActivated(d =>
            {
                d(this.BindCommandCustom(BackButton, v => v.ViewModel.NavigateBackCommand));
                d(this.OneWayBind(ViewModel, vm => vm.PageGlyph, v => v.IconLabel.Text));
                d(this.OneWayBind(ViewModel, vm => vm.PageTitle, v => v.TitleLabel.Text));
                d(this.OneWayBind(ViewModel, vm => vm.SecondaryPageTitle, v => v.SubTitleLabel.Text));
                d(this.BindCommandCustom(HomeButton, v => v.ViewModel.NavigateHomeCommand));
                d(this.BindCommandCustom(UserSettingsButton, v => v.ViewModel.NavigateToUserSettingsCommand));
                d(this.BindCommandCustom(MenuButton, v => v.ViewModel.ShowMenuCommand));
                d(this.OneWayBind(ViewModel, vm => vm.SectionTitlePlayerViewModel, v => v.SectionTitlePlayer.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, v => v.BackButton.Text, SetBackButtonDirection));

                d(this
                    .WhenAnyValue(page => page.ViewModel.ShowBackButton)
                    .Subscribe(showBackButton => ChangeControlVisibility(
                        makeVisible: showBackButton, 
                        controlColumn: BackButtonColumn, 
                        separatorColumn: SeparatorColumn1, 
                        size: new GridLength(Size))));
                
                d(this
                    .WhenAnyValue(page => page.ViewModel.ShowSectionPlayer)
                    .Subscribe(showSectionPlayer => ChangeControlVisibility(
                        makeVisible: showSectionPlayer 
                                     && int.TryParse(ViewModel.SectionTitlePlayerViewModel.SectionNumber, out int sectionNumber)
                                     && sectionNumber > 0, 
                        controlColumn: SectionPlayerColumn, 
                        separatorColumn: SeparatorColumn3,
                        size: GridLength.Auto)));
                
                d(this
                    .WhenAnyValue(page => page.ViewModel.ShowSettings)
                    .Subscribe(showSettings => ChangeControlVisibility(
                        makeVisible: showSettings, 
                        controlColumn: UserSettingsColumn, 
                        separatorColumn: SeparatorColumn4,
                        size: new GridLength(Size))));
                
                d(this
                    .WhenAnyValue(page => page.ViewModel.ShowLogo)
                    .Subscribe(showLogo => ChangeControlVisibility(
                        makeVisible: showLogo, 
                        controlColumn: HomeButtonColumn, 
                        separatorColumn: SeparatorColumn5,
                        size: new GridLength(Size))));

                d(this
                    .WhenAnyValue(page => page.ViewModel.ShowPageTitleIcon)
                    .Subscribe(showTitleIcon =>
                    {
                        IconColumn.Width = showTitleIcon ? IconWidth : 0;
                        SectionTitleGrid.Margin = showTitleIcon ? new Thickness(Spacing, 0, 0, 0) : 0;
                    }));

                d(this
                    .WhenAnyValue(page => page.ViewModel.IsEnabled)
                    .Subscribe(isEnabled => ChangeControlAvailability(isEnabled)));
            });
    }

    private string SetBackButtonDirection(FlowDirection flowDirection)
    {
        return flowDirection == FlowDirection.RightToLeft
            ? IconExtensions.GetIconGlyph(Icon.ChevronRight)
            : IconExtensions.GetIconGlyph(Icon.ChevronLeft);
    }
        
    private void ChangeControlVisibility(
        bool makeVisible, 
        ColumnDefinition controlColumn, 
        ColumnDefinition separatorColumn, 
        GridLength size)
    {
        controlColumn.Width = makeVisible ? size : 0;
        separatorColumn.Width = makeVisible ? Separator.Width : 0;
    }
    
    private void ChangeControlAvailability(bool isEnabled)
    {
        BackButton.IsEnabled = isEnabled;
        HomeButton.IsEnabled = isEnabled;
        MenuButton.IsEnabled = isEnabled;
        UserSettingsButton.IsEnabled = isEnabled;
        SectionTitlePlayer.IsEnabled = isEnabled;
    }
        
    protected override void Dispose(bool disposing)
    {
        SectionTitlePlayer?.Dispose();
        ViewModel?.Dispose();

        base.Dispose(disposing);
    }
}