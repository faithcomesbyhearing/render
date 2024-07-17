using ReactiveUI;
using Render.Resources.Localization;

namespace Render.Components.SectionTitlePlayer
{
    public partial class SectionTitlePlayer
    {
        public const double OverlayHeight = 45;

        public static readonly BindableProperty TitleBarHeightProperty = BindableProperty.Create(
            nameof(TitleBarHeight),
            typeof(double),
            typeof(SectionTitlePlayer));

        public static readonly BindableProperty SpacingProperty = BindableProperty.Create(
            nameof(Spacing),
            typeof(double),
            typeof(SectionTitlePlayer));

        private readonly IDisposable _disposables;

        public double TitleBarHeight
        {
            get => (double)GetValue(TitleBarHeightProperty);
            set => SetValue(TitleBarHeightProperty, value);
        }

        public double Spacing
        {
            get => (double)GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        public SectionTitlePlayer()
        {
            InitializeComponent();

            _disposables = this.WhenActivated(d =>
            {
                d(this.BindCommand(ViewModel, vm => vm.ButtonClickCommand, v => v.sectionNumberAudioPlay));
                d(this.BindCommand(ViewModel, vm => vm.ButtonClickCommand, v => v.sectionNumberAudioStop));

                d(this.WhenAnyValue(
                        flow => flow.ViewModel.FlowDirection)
                        .Subscribe(flow =>
                        {
                            passageIconLabel.Rotation = FlipIcon(flow);
                        }));

                d(this.OneWayBind(
                    viewModel: ViewModel,
                    vmProperty: vm => vm.PassageNumber,
                    viewProperty: v => v.passageNumberOverlayContainer.IsVisible,
                    selector: passageNumber => !string.IsNullOrEmpty(passageNumber)));

                d(this.OneWayBind(
                    viewModel: ViewModel,
                    vmProperty: vm => vm.PassageNumber,
                    viewProperty: v => v.passageNumberLabel.Text,
                    selector: passageNumber => string.Format(AppResources.Passage, passageNumber)));

                d(this
                    .WhenAnyValue(
                        page => page.ViewModel.HasAudio,
                        page => page.ViewModel.IsPlaying)
                    .Subscribe(((bool hasAudio, bool isPlaying) values) =>
                    {
                        var (hasAudio, isPlaying) = values;

                        sectionNumberAudioPlay.IsVisible = hasAudio && !isPlaying;
                        sectionNumberAudioStop.IsVisible = hasAudio && isPlaying;
                    }));

                d(this
                    .WhenAnyValue(page => page.ViewModel.SectionNumber)
                    .Subscribe(number => sectionNumberLabel.Text = string.Format(AppResources.Section, number)));

                d(this
                    .WhenAnyValue(page => page.TitleBarHeight)
                    .Subscribe(titleBarHeight =>
                    {
                        var overlayTranslationY = (0.5 * titleBarHeight) + (0.5 * OverlayHeight) - 2;
                        passageNumberOverlayContainer.TranslationY = overlayTranslationY;
                    }));

                d(this
                    .WhenAnyValue(page => page.Spacing)
                    .Subscribe(spacing =>
                    {
                        var thickness = new Thickness { Left = spacing, Right = spacing };
                        overlayLayout.Padding = thickness;
                        sectionNumberPlayer.Padding = thickness;
                    }));
            });
        }

        private int FlipIcon(FlowDirection flowDirection)
        {
            return flowDirection == FlowDirection.RightToLeft
                ? 180
                : 0;
        }

        protected override void Dispose(bool disposing)
        {
            _disposables?.Dispose();
            ViewModel?.Dispose();

            base.Dispose(disposing);
        }
    }
}