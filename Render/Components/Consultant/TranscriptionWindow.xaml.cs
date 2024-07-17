using System.Reactive.Linq;
using ReactiveUI;
using Render.Resources;

namespace Render.Components.Consultant
{
    public partial class TranscriptionWindow
    {
        private readonly Color _optionalColor;
        private readonly Color _grayColor;
        public TranscriptionWindow()
        {
            InitializeComponent();

            _optionalColor = ResourceExtensions.GetColor("Option");
            _grayColor = ResourceExtensions.GetColor("AlternateBackground");

            this.WhenActivated(d =>
            {
                d(this.BindCommand(ViewModel, vm => vm.IncreaseFontSizeCommand, 
                    v => v.IncreaseFontTap));
                d(this.BindCommand(ViewModel, vm => vm.DecreaseFontSizeCommand, 
                    v => v.DecreaseFontTap));
                
                d(this.BindCommand(ViewModel, vm => vm.CopyToClipBoardCommand, 
                    v => v.CopyTextTap));

                d(this.WhenAnyValue(x => x.ViewModel.Transcriptions.Items)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(transcriptions =>
                    {
                        var source = BindableLayout.GetItemsSource(Transcriptions);
                        if (source == null)
                        {
                            BindableLayout.SetItemsSource(Transcriptions, transcriptions);
                        }
                    }));

                d(this.WhenAnyValue(x => x.ViewModel.ClipBoardButtonIsClicked)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(SetLineColor));
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, 
                    v => v.TopLevelElement.FlowDirection));
            });
        }
        
        private void SetLineColor(bool isSelected)
        {
            Transcriptions.SetValue(BackgroundColorProperty, isSelected ? _optionalColor : _grayColor);
        }
    }
}