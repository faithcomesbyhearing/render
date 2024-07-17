using CommunityToolkit.Maui.Views;
using Render.Components.TitleBar;
using Render.Kernel.CustomRenderer;

namespace Render.Kernel.WrappersAndExtensions
{
    public class MenuPopupService : IMenuPopupService
    {
        private TitleBarMenu _popupContentView;
        private CustomPopup _popup;

        public void Show(TitleBarMenuViewModel viewModel)
        {
            _popupContentView = new TitleBarMenu { BindingContext = viewModel };
            _popupContentView.Loaded += PopupContentViewOnLoaded;

            _popup = new CustomPopup()
            {
                Content = _popupContentView,
                Color = Colors.Transparent,
                HorizontalOptions = MenuLayoutAlighnment(viewModel.FlowDirection),
                VerticalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Start
            };

            var page = Application.Current?.MainPage ?? throw new NullReferenceException();

            page.ShowPopup(_popup);
        }

        private void PopupContentViewOnLoaded(object sender, EventArgs e)
        {
            // Set a specific _popup height height after _popupContentView is loaded.
            // Because CommunityToolkit Popup control does not calculate it's height automatically.
            _popup.Size = new Size(_popupContentView.MenuWidth, _popupContentView.MenuHeight);
            _popupContentView.Loaded -= PopupContentViewOnLoaded;
        }

        private Microsoft.Maui.Primitives.LayoutAlignment MenuLayoutAlighnment(FlowDirection flowDirection)
        {
            return flowDirection == FlowDirection.RightToLeft
                ? Microsoft.Maui.Primitives.LayoutAlignment.Start
                : Microsoft.Maui.Primitives.LayoutAlignment.End;
        }
        public void Close()
        {
            _popup?.Close();
        }
    }
}