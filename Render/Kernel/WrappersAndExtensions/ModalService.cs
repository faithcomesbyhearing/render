using CommunityToolkit.Maui.Views;
using ReactiveUI;
using Render.Components.Modal;
using Render.Kernel.CustomRenderer;
using Render.Resources;
using Render.Resources.Localization;
using Render.Utilities;

namespace Render.Kernel.WrappersAndExtensions
{
    /// <summary>
    /// Updated according to:
    /// https://dev.azure.com/FCBH/Software%20Development/_workitems/edit/9337
    /// </summary>
    public class ModalService : IModalService
    {
        private Modal _modalView;
        private ModalViewModel _modalViewModel;
        private CustomPopup _popup;

        private TaskCompletionSource<DialogResult> _taskCompletionSource;
        private Task<DialogResult> PopupClosedTask => _taskCompletionSource.Task;

        private readonly IViewModelContextProvider _viewModelContextProvider;

        public ModalService(IViewModelContextProvider viewModelContextProvider)
        {
            _viewModelContextProvider = viewModelContextProvider;
        }

        public async Task<DialogResult> ShowInfoModal(
            Icon icon,
            string title,
            string message)
        {
            return await ShowInfoModal(icon, title, message, new ModalButtonViewModel(AppResources.OK));
        }

        public async Task<DialogResult> ShowInfoModal(
            Icon icon,
            string title,
            string message,
            ModalButtonViewModel okButtonViewModel,
            Func<Task> onClosed = null,
            Func<Task> onOkPressed = null)
        {
            var modalViewModel = new ModalViewModel(_viewModelContextProvider, this, icon, title, message, okButtonViewModel, onClosed);

            if (onOkPressed != null)
            {
                modalViewModel.AfterConfirmCommand = ReactiveCommand.CreateFromTask(onOkPressed);
            }

            RenderLogger.LogInfo("Info Modal", new Dictionary<string, string>
            {
                { "Title", title }
            });

            ShowModal(modalViewModel);
            return await PopupClosedTask;
        }

        public async Task<DialogResult> ConfirmationModal(
            Icon icon,
            string title,
            string message,
            string cancelText,
            string confirmText)
        {
            return await ConfirmationModal(icon, title, message, new ModalButtonViewModel(cancelText), new ModalButtonViewModel(confirmText));
        }

        public async Task<DialogResult> ConfirmationModal(
            Icon icon,
            string title,
            string message,
            ModalButtonViewModel cancelButtonViewModel,
            ModalButtonViewModel confirmButtonViewModel)
        {
            var modalViewModel = new ModalViewModel(
                _viewModelContextProvider,
                this,
                icon,
                title,
                message,
                cancelButtonViewModel,
                confirmButtonViewModel);

            RenderLogger.LogInfo("Confirmation Modal", new Dictionary<string, string>
            {
                { "Title", title }
            });

            ShowModal(modalViewModel);
            return await PopupClosedTask;
        }

        public async Task<DialogResult> ConfirmationModal(
            Icon icon,
            string title,
            ViewModelBase contentViewModel,
            ModalButtonViewModel cancelButtonViewModel,
            ModalButtonViewModel confirmButtonViewModel)
        {
            var modalViewModel = new ModalViewModel(_viewModelContextProvider, this, icon, title, contentViewModel, cancelButtonViewModel, confirmButtonViewModel);

            RenderLogger.LogInfo("Confirmation Modal with content", new Dictionary<string, string>
            {
                { "Title", title },
                { "Content", contentViewModel?.GetType().Name}
            });

            ShowModal(modalViewModel);
            return await PopupClosedTask;
        }

        public async Task<DialogResult> ConfirmationModal(ModalViewModel modalViewModel)
        {
            ShowModal(modalViewModel);
            return await PopupClosedTask;
        }

        private void ShowModal(ModalViewModel modalViewModel)
        {
            _taskCompletionSource = new TaskCompletionSource<DialogResult>();
            _modalViewModel = modalViewModel;

            _modalView = new Modal()
            {
                BindingContext = _modalViewModel
            };

            _popup = new CustomPopup()
            {
                Content = _modalView,
                Color = Colors.Transparent,
                CanBeDismissedByTappingOutsideOfPopup = false,
                HorizontalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Fill,
                VerticalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Fill
            };

            var page = Application.Current?.MainPage ?? throw new NullReferenceException();
            page.ShowPopup(_popup);
        }

        public void Close(DialogResult result)
        {
            if (_popup is null) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _popup.Close();
            });

            _modalViewModel?.Dispose();
            _modalViewModel = null;
            _popup = null;

            _taskCompletionSource.SetResult(result);
        }
    }
}