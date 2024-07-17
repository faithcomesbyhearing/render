using ReactiveUI;

namespace Render.Pages.Transcribe.TranscribeRetellBackTranslate;

public partial class TranscribeTabletRetellPassageTranslatePage
{
    public TranscribeTabletRetellPassageTranslatePage()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, 
                v => v.TopLevelElement.FlowDirection));
            d(this.OneWayBind(ViewModel, vm => vm.BarPlayerViewModel, 
                v => v.BarPlayer.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, 
                v => v.TitleBar.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.ProceedButtonViewModel, 
                v => v.ProceedButton.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.TranscribeTextBoxViewModel,
                v => v.TextBox.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.IsLoading, 
                v => v.LoadingView.IsVisible));
        });
    }

    //Pause all the bar players on page exit
    protected override void OnDisappearing()
    {
        if (ViewModel != null)
        {
            ViewModel.LoopAudio = false;
            
            ViewModel.PauseAudio();
            
            ViewModel.PauseSectionTitlePlayer();
        }
        
        base.OnDisappearing();
    }

    protected override void Dispose(bool disposing)
    {
        TextBox.Dispose();

        base.Dispose(disposing);
    }
}