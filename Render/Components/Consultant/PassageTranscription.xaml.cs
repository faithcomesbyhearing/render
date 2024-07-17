using System.Reactive.Linq;
using ReactiveUI;
using Render.Resources;

namespace Render.Components.Consultant
{
    public partial class PassageTranscription
    {
        private readonly Color _optionalColor;
        private readonly Color _grayColor;
        private readonly Color _mainTextColor;
        private readonly Color _highlightTextColor;
        public PassageTranscription()
        {
            InitializeComponent();
            
            _optionalColor = ResourceExtensions.GetColor("Option");
            _grayColor = ResourceExtensions.GetColor("AlternateBackground");
            _mainTextColor = ResourceExtensions.GetColor("MainIconColor");
            _highlightTextColor = ResourceExtensions.GetColor("DraftSelectionSeparatorRequired");
            
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.Transcription,
                    v => v.TranscriptionTextBox.Text));
                d(this.OneWayBind(ViewModel, vm => vm.FontSize,
                    v => v.TranscriptionTextBox.FontSize));
                d(this.OneWayBind(ViewModel, vm => vm.Number,
                    v => v.PassageNumber.Text));
                d(this.OneWayBind(ViewModel, vm => vm.ShowPassageIcon,
                    v => v.PassageIcon.IsVisible));

                d(this.WhenAnyValue(x => x.ViewModel.ShowPassageIcon)
                    .Subscribe(SetPassageNumberText));
                d(this.WhenAnyValue(x => x.ViewModel.IsSelected)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(SetLineColor));
                d(this.WhenAnyValue(x => x.ViewModel.FlowDirection)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(flowDirection =>
                    {
                        if (flowDirection == FlowDirection.LeftToRight)
                        {
                            PassageIcon.SetValue(MarginProperty, new Thickness(0,0,5,0));
                        }
                        else
                        {
                            PassageIcon.SetValue(MarginProperty, new Thickness(5,0,0,0));
                            PassageIcon.SetValue(RotationProperty, 180);
                        }
                    }));
            });
        }
        
        private void SetLineColor(bool isSelected)
        {
            TranscriptionTextBox.TextColor = isSelected ? _grayColor : _mainTextColor;
            PassageNumber.TextColor = isSelected ? _highlightTextColor : _mainTextColor;
            TopLevelElement.SetValue(BackgroundColorProperty, isSelected? _optionalColor : _grayColor);
        }

        private void SetPassageNumberText(bool showPassageIcon)
        {
            PassageNumber.SetValue(Label.TextProperty, ViewModel?.Number);
        }
    }
}