using System.Reactive.Linq;
using ReactiveUI;

namespace Render.Components.TranscribeTextBox
{
    public partial class TranscribeTextBoxController : IDisposable
    {
        private double _resizeOffset = 50;
        private double _lastWindowHeight;

        public TranscribeTextBoxController()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.BindCommand(ViewModel, vm => vm.IncreaseFontSizeCommand, v => v.IncreaseFontTap));
                d(this.BindCommand(ViewModel, vm => vm.DecreaseFontSizeCommand, v => v.DecreaseFontTap));
                d(this.BindCommand(ViewModel, vm => vm.CopyTextCommand, v => v.CopyTextTap));
                d(this.OneWayBind(ViewModel, vm => vm.FontSize, v => v.TextField.FontSize));
                d(this.Bind(ViewModel, vm => vm.Input, v => v.TextField.Text));
                d(this.WhenAnyValue(x => x.ViewModel.Input).Subscribe(SetupIconColors));
            });

            _lastWindowHeight = Application.Current.MainPage.Window.Height;
            Application.Current.MainPage.Window.SizeChanged += WindowSizeChanged;
        }

        private void WindowSizeChanged(object sender, EventArgs e)
        {
            if (sender is not Window window)
            {
                return;
            }

            TryFocusTextField(window.Height);

            _lastWindowHeight = window.Height;
        }

        /// <summary>
        /// MAUI Editor has unresolved issue with invalid text content size calculation
        /// inside MAUI layout. See details here: https://github.com/dotnet/maui/issues/12458
        /// 
        /// To tackle this problem need to force size recalculation for inner scroll view.
        /// The most easiest way to do that is force focus on Editor control.
        /// 
        /// This workaround is aplied only in case when the user presses maximize\restore window system button.
        /// No issues when the user changes the size of the window by dragging window edge.
        /// 
        /// To detect whether the user maximizes\restores window or changes window size by dragging window edge,
        /// need to track window size delta and compare it with the offset.
        /// </summary>
        private void TryFocusTextField(double currentWindowHeight)
        {
            if (Math.Abs(_lastWindowHeight - currentWindowHeight) <= _resizeOffset)
            {
                return;
            }

            TextField.Focus();
        }

        private void SetupIconColors(string input)
        {
            if (input == string.Empty)
            {
                CopyButtonBorder.SetValue(OpacityProperty, 0.3);
                IncreaseFont.SetValue(OpacityProperty, 0.3);
                DecreaseFont.SetValue(OpacityProperty, 0.3);
            }
            else
            {
                CopyButtonBorder.SetValue(OpacityProperty, 1);
                IncreaseFont.SetValue(OpacityProperty, 1);
                DecreaseFont.SetValue(OpacityProperty, 1);
            }
        }

        public void Dispose()
        {
            Application.Current.MainPage.Window.SizeChanged -= WindowSizeChanged;
        }
    }
}