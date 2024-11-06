using ReactiveUI;

namespace Render.Components.SectionInfo;

public partial class SectionInfoPlayers
{
    public SectionInfoPlayers()
    {
        InitializeComponent();
        DisposableBindings = this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.DraftAudioPlayer, v => v.DraftAudioBarPlayer.BindingContext));
            d(this.WhenAnyValue(v => v.ViewModel.RetellAudioPlayers)
                .Subscribe(observableCollection =>
                {
                    BindableLayout.SetItemsSource(SectionAudioBarPlayerCollection, observableCollection);
                }));
        });
    }
}