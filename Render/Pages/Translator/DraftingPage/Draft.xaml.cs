using ReactiveUI;
using System.Reactive.Linq;
using Render.Resources;
using Render.Resources.Styles;

namespace Render.Pages.Translator.DraftingPage
{
    public partial class Draft
    {
        private Icon _icon;
        private ColorReference _iconColor;
        private ColorReference _previousDraftIconColor;
        public Draft()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.Number, v => v.Label.Text));
                d(this.BindCommand(ViewModel, vm => vm.SelectCommand, v => v.Tap));
                d(this.WhenAnyValue(x => x.ViewModel.Selected).Subscribe(ChangeFrame));
                d(this.WhenAnyValue(x => x.ViewModel.DraftState).ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(DraftIconSelector));
                d(this.OneWayBind(ViewModel, vm => vm.IsPreviousDraft, v => v.PreviousDraftStar.IsVisible));
            });
        }
        private void DraftIconSelector(DraftState arg)
        {
            _icon = arg == DraftState.HasAudio ? Icon.DraftsNew : Icon.Add;
            SetIconColor(_iconColor,_previousDraftIconColor);
        }
        private void ChangeFrame(bool selected)
        {
            if (selected)
            {
                _iconColor =  (ColorReference)ResourceExtensions.GetResourceValue("OptionAudioPlayerBackground");
                _previousDraftIconColor = _iconColor;
                var frameColor = ((ColorReference)ResourceExtensions.GetResourceValue("Option")).Color;
                var insideColor = ((ColorReference)ResourceExtensions.GetResourceValue("Option")).Color;
                SetFrameColor(frameColor, insideColor);
            }
            else
            {
                _iconColor =  (ColorReference)ResourceExtensions.GetResourceValue("Option");
                _previousDraftIconColor = (ColorReference)ResourceExtensions.GetResourceValue("MainIconColor");
                var frameColor = ((ColorReference)ResourceExtensions.GetResourceValue("DraftDeselectedOutline")).Color;
                var insideColor = ((ColorReference)ResourceExtensions.GetResourceValue("OptionAudioPlayerBackground")).Color;
                SetFrameColor(frameColor, insideColor);
            }

            SetIconColor(_iconColor, _previousDraftIconColor.Color);
        }

        private void SetFrameColor(Color frameColor, Color insideFrameColor)
        {
            BorderOutside.SetValue(Microsoft.Maui.Controls.Frame.BorderColorProperty, frameColor);
            BorderOutside.SetValue(BackgroundColorProperty, frameColor);
            InsideLayout.SetValue(BackgroundColorProperty, insideFrameColor);
            BorderInside.SetValue(Microsoft.Maui.Controls.Frame.BorderColorProperty, frameColor);
            BorderInside.SetValue(BackgroundColorProperty, frameColor);
        }

        private void SetIconColor(Color iconColor, Color previousDraftIconColor)
        {
            var fontImage = IconExtensions.BuildFontImageSource(_icon,iconColor);
            var draftImageLabel = new Label
            {
                Text = fontImage.Glyph,
                AutomationId = $"{_icon.ToString()}Icon",
                TextColor = iconColor,
                FontFamily = "Icons",
                FontSize = _icon == Icon.DraftsNew ? 50 : 70
            };
            
            DraftImage.Children.Clear();
            DraftImage.Children.Add(draftImageLabel);
            Label.TextColor = iconColor;
            PreviousDraftStar.SetValue(Label.TextColorProperty, previousDraftIconColor);
        }
    }
}