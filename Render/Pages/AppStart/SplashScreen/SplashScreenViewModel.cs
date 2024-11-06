using Render.Kernel;
using Render.Pages.AppStart.Login;
using Render.Repositories.LocalDataRepositories;

namespace Render.Pages.AppStart.SplashScreen
{
    public class SplashScreenViewModel : PageViewModelBase
    {
        private readonly ILocalProjectsRepository _localProjectDataRepository;

        public SplashScreenViewModel(IViewModelContextProvider viewModelContextProvider) :
            base("SplashScreen", viewModelContextProvider, "Splash")
        {
            _localProjectDataRepository = viewModelContextProvider.GetLocalProjectsRepository();
        }

        public async Task NavigateToLoginAsync()
        {
            try
            {
                //Clean up audio before we start really doing work
                await ViewModelContextProvider.CompactDatabasesAsync();
                
                var localData = await _localProjectDataRepository.GetLocalProjectsForMachine();
                if (!localData.GetProjectIds().Any())
                {
                    var vm = new AddVesselUserLoginViewModel(ViewModelContextProvider);
                    await NavigateToAndReset(vm);
                    return;
                }

                var loginViewModel = await LoginViewModel.CreateAsync(ViewModelContextProvider);
                await NavigateToAndReset(loginViewModel);
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }

        public override void Dispose()
        {
            _localProjectDataRepository?.Dispose();
            base.Dispose();
        }
    }
}