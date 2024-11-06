using Render.Components.Modal;
using Render.Resources;

namespace Render.Kernel.WrappersAndExtensions
{
    public interface IModalService
    {
        Task<DialogResult> ShowInfoModal(
            Icon icon,
            string title,
            string message);

        Task<DialogResult> ShowInfoModal(
            Icon icon,
            string title,
            string message,
            ModalButtonViewModel okButtonViewModel,
            Func<Task> onClosed = null,
            Func<Task> onOkPressed = null);

        Task<DialogResult> ConfirmationModal(
            Icon icon,
            string title,
            string message,
            string cancelText,
            string confirmText);

        Task<DialogResult> ConfirmationModal(
            Icon icon,
            string title,
            string message,
            ModalButtonViewModel cancelButtonViewModel,
            ModalButtonViewModel confirmButtonViewModel);

        Task<DialogResult> ConfirmationModal(
            Icon icon,
            string title,
            ViewModelBase contentViewModel,
            ModalButtonViewModel cancelButtonViewModel,
            ModalButtonViewModel confirmButtonViewModel);

        Task<DialogResult> ConfirmationModal(ModalViewModel modalViewModel);

        void Close(DialogResult result);
    }
}