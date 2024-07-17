using Render.Resources;
using Render.Resources.Localization;
using Render.Utilities;

namespace Render.Kernel
{
    public static class ErrorManager
    {
        public static async Task ShowErrorPopupAsync(IViewModelContextProvider viewModelContextProvider, Exception ex)
        {
            await viewModelContextProvider.GetModalService().ShowInfoModal(
                icon: Icon.TypeWarning,
                title: AppResources.Error,
                message: AppResources.ErrorOccuredTryAgain);
            
            RenderLogger.LogError(ex);
        }
        
        public static void ShowErrorPopup(IViewModelContextProvider viewModelContextProvider, Exception ex)
        {
            viewModelContextProvider.GetModalService().ShowInfoModal(
                icon: Icon.TypeWarning,
                title: AppResources.Error,
                message: AppResources.ErrorOccuredTryAgain);
            
            RenderLogger.LogError(ex);
        }
    }
}