using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Pages.AppStart.Login;
using Render.Resources;
using Render.Resources.Localization;

namespace Render.Components.TitleBar.MenuActions
{
    public class LogOutActionViewModel : MenuActionViewModel
    {
        public LogOutActionViewModel(IViewModelContextProvider viewModelContextProvider, string pageName) : base("LogOutAction",
            viewModelContextProvider, pageName)
        {
            var imageSource = (FontImageSource)ResourceExtensions.GetResourceValue("LogOutIcon");
            var title = AppResources.LogOut;
            var command = ReactiveCommand.CreateFromTask(async () =>
            {
                IsActionExecuting = true;
                CloseMenu();

                if (CanActionExecute)
                {
                    return await NavigateToAsync();
                }

                Disposables.Add(this.WhenAnyValue(vm => vm.CanActionExecute)
                    .Subscribe(async canActionExecute =>
                    {
                        if (IsActionExecuting && canActionExecute)
                        {
                            await NavigateToAsync();
                        }
                    }));

                return HostScreen.Router.GetCurrentViewModel();

            });
            SetSources(imageSource, command, title);
        }

        private async Task<IRoutableViewModel> NavigateToAsync()
        {
            try
            {
                LogInfo("LogOut", new Dictionary<string, string>
                {
                    { "Username", ViewModelContextProvider.GetLoggedInUser()?.Username }
                });

                ViewModelContextProvider.GetLocalSyncService().StopLocalSync();
                var loginViewModel = await Task.Run(async () => await LoginViewModel.CreateAsync(ViewModelContextProvider));
                ViewModelContextProvider.ClearLoggedInUser();
                IsActionExecuting = false;
                return await HostScreen.Router.NavigateAndReset.Execute(loginViewModel);
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }
    }
}