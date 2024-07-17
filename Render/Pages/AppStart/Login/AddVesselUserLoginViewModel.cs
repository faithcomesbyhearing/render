using Microsoft.AspNetCore.Identity;
using ReactiveUI;
using Render.Kernel;
using Render.Resources;
using Render.Resources.Localization;
using Render.Services.PasswordServices;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI.Fody.Helpers;
using Render.Components.AddViaFolder;
using Render.Repositories.Extensions;

namespace Render.Pages.AppStart.Login
{
    public class AddVesselUserLoginViewModel : LoginViewModelBase
    {
        public ReactiveCommand<Unit, IRoutableViewModel> NavigateToAddProjectId { get; set; }
        
        public AddFromComputerViewModel AddFromComputerViewModel { get; }
        public ReactiveCommand<Unit, Unit> AddProjectFromComputerCommand { get; private set; }
        
        public bool ShowBackButton { get; }
        public bool ShowAddNewUserLabel { get; }
        
        [Reactive] public bool AllowLoginCommand { get; private set; }
        
        public AddVesselUserLoginViewModel(IViewModelContextProvider viewModelContextProvider)
            : base("AddVesselUserLogin", viewModelContextProvider, "Add Vessel User Login")
        {
            LoginCommand = ReactiveCommand.CreateFromTask(TryLoginAsync, this.WhenAnyValue(x => x.AllowLoginCommand));
            NavigateBackCommand = ReactiveCommand.CreateFromTask(GoBackAsync);
            UsernameViewModel.OnEnterCommand = ReactiveCommand.CreateFromTask(async () => { await TryLoginAsync(); });
            PasswordViewModel.OnEnterCommand = ReactiveCommand.CreateFromTask(async () => { await TryLoginAsync(); });
            NavigateToAddProjectId = ReactiveCommand.CreateFromTask(NavigateToAddAProjectViaIdAsync);
            
            AddFromComputerViewModel = new AddFromComputerViewModel(viewModelContextProvider, AppResources.ReturnToLogIn, NavigateToLogin);
            AddProjectFromComputerCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await AddFromComputerViewModel.OpenFileAndStartImport();
            });
            
            //Hide the back button on initial load of Render otherwise we have navigated to this page from another page then show button.
            ShowBackButton = HostScreen?.Router?.NavigationStack.Count > 0 && HostScreen?.Router?.NavigationStack.First()?.UrlPathSegment != "SplashScreen";
            ShowAddNewUserLabel = ShowBackButton;
            
            Disposables.Add(this.WhenAnyValue(x => x.UsernameViewModel.Value, x => x.PasswordViewModel.Value).Subscribe(x =>
            {
                AllowLoginCommand = !x.Item1.IsNullOrEmpty() && !x.Item2.IsNullOrEmpty();
            }));
        }

        private async Task<IRoutableViewModel> NavigateToAddAProjectViaIdAsync()
        {
            IsLoading = true;
            var vm = await Task.Run(async () => await AddProjectViaIdViewModel.CreateAsync(ViewModelContextProvider));
            PasswordViewModel.IsPassword = true;
            var result = await NavigateTo(vm);
            IsLoading = false;

            return result;
        }

        private async Task<IRoutableViewModel> NavigateToLogin()
        {
            IsLoading = true;
            var loginViewModel = await LoginViewModel.CreateAsync(ViewModelContextProvider);
            await loginViewModel.GetAllUsersForLoginAsync();
            IsLoading = false;
            return await NavigateToAndReset(loginViewModel);
        }
        public async Task<IRoutableViewModel> TryLoginAsync()
        {
            UsernameViewModel.ClearValidation();
            PasswordViewModel.ClearValidation();

            UsernameViewModel.Value = UsernameViewModel.Value?.Trim();
            PasswordViewModel.Value = PasswordViewModel.Value?.Trim();

            if (string.IsNullOrWhiteSpace(UsernameViewModel.Value) ||
                string.IsNullOrWhiteSpace(PasswordViewModel.Value))
            {
                if (string.IsNullOrWhiteSpace(UsernameViewModel.Value))
                {
                    UsernameViewModel.SetValidation(AppResources.NullUsername);
                }

                if (string.IsNullOrWhiteSpace(PasswordViewModel.Value))
                {
                    PasswordViewModel.SetValidation(AppResources.EmptyPassword);
                }

                return null;
            }

            var username = UsernameViewModel.Value;
            var userOnMachine = await UserRepository.GetUserAsync(username);
			Loading = true;
			if (userOnMachine == null)
            {
                var result =
                    await AuthenticationApiWrapper.AuthenticateUserAsync(UsernameViewModel.Value,
                        PasswordViewModel.Value);
                if (result.SignInResult)
                {
					async Task afterLogin()
					{
						ShowAddProjectIdButton = false;
						await SynchronizeAdmin(UsernameViewModel.Value, PasswordViewModel.Value);
					}
					if (await CheckForNewSoftwareVersionAsync(null, afterLogin))
					{
						return null;
					}

					await afterLogin();
				}
                else
                {
                    Loading = false;
                    if (!result.OfflineError)
                    {
                        PasswordViewModel.SetValidation(AppResources.IncorrectPassword);
                    }
                    else
                    {
                        await ShowNoConnectionModal();
                    }
                }
            }
            else
            {
                if (!UsernameViewModel.CheckValidation())
                {
                    UsernameViewModel.ClearValidation();
                }

                var validatorFactory = new PasswordValidatorFactory();
                var validator = validatorFactory.GetValidator(userOnMachine);
                var result = validator.ValidatePassword(userOnMachine, PasswordViewModel.Value);
                if (result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    var newHash = validator.HashPassword(userOnMachine, PasswordViewModel.Value);
                    userOnMachine.HashedPassword = newHash;
                    await UserRepository.SaveUserAsync(userOnMachine);
                }
                if (result != PasswordVerificationResult.Failed)
				{
#if DEMO
                    MainThread.BeginInvokeOnMainThread(FinishLoginOnMainThread);
                    return null;
#endif

					if (!await ViewModelContextProvider.GetSyncGatewayApiWrapper().IsConnected())
					{
						Loading = false;
						await ShowNoConnectionModal();
						return null;
					}

					async Task afterLogin()
					{
						await SynchronizeAdmin(UsernameViewModel.Value, PasswordViewModel.Value);
					}
					if (await CheckForNewSoftwareVersionAsync(userOnMachine, afterLogin))
					{
						return null;
					}

					await afterLogin();
					return null;
				}

				Loading = false;
				PasswordViewModel.SetValidation(AppResources.IncorrectPassword);
                LogInfo("Login failed", new Dictionary<string, string>
                {
                    {"User", userOnMachine.Username}
                });
            }

            return null;
		}

        private async Task ShowNoConnectionModal()
        {
            await ViewModelContextProvider.GetModalService().ShowInfoModal(
                Icon.InternetError,
                AppResources.NoInternetConnection,
                AppResources.PleaseConnect);
        }

        private async Task<IRoutableViewModel> GoBackAsync()
        {
            var stack = Application.Current?.MainPage?.Navigation?.NavigationStack;
            if (stack is null || stack.Count is 1)
            {
                return await Observable.Empty<IRoutableViewModel>();
            }

            PasswordViewModel.Value = string.Empty;
            PasswordViewModel.ClearValidation();

            return await NavigateBack();
        }
        

        public override void Dispose()
        {
            NavigateToAddProjectId?.Dispose();
            AddFromComputerViewModel?.Dispose();
            base.Dispose();
        }
    }
}
