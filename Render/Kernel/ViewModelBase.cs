using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Models.Workflow;
using Render.Pages.AppStart.Home;
using Render.Services.SessionStateServices;
using Splat;
using Render.Resources.Localization;
using Render.Kernel.WrappersAndExtensions;
using Render.Extensions;
using Render.Interfaces;
using Render.Models.Workflow.Stage;

namespace Render.Kernel
{
    public class ViewModelBase : ReactiveObject, IRoutableViewModel, IViewModelBase
    {
        private IRenderLogger _logger;
        protected IRenderLogger Logger
        {
            get
            {
                _logger ??= ViewModelContextProvider.GetLogger(GetType());
                return _logger;
            }
        }

        [Reactive] public bool IsLoading { get; set; }
        public IScreen HostScreen { get; set; }
        public FlowDirection FlowDirection { get; }
        public string UrlPathSegment { get; }

        // need to make ViewModelContextProvider public so we can access it
        // using reflection in RenderPageBase.cs
        public IViewModelContextProvider ViewModelContextProvider { get; }

        protected ISessionStateService SessionStateService { get; }

        private Guid UserId { get; }
        private Guid ProjectId { get; }

        public ViewModelBase(string urlPathSegment, IViewModelContextProvider viewModelContextProvider, IScreen screen = null)
        {
            ViewModelContextProvider = viewModelContextProvider;
            SessionStateService = ViewModelContextProvider.GetSessionStateService();
            UrlPathSegment = urlPathSegment;
            HostScreen = screen ?? Locator.Current.GetService<IScreen>();
            var currentCulture = viewModelContextProvider.GetLocalizationService().GetCurrentLocalization();
            FlowDirection = currentCulture == "ar"
                ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            NavigateBackCommand = ReactiveCommand.CreateFromTask(NavigateBack);
        }

        public async Task<IRoutableViewModel> NavigateTo(ViewModelBase vm)
        {
            await OnNavigatingAwayAsync();

            LogInfo("Navigation", new Dictionary<string, string>
            {
                { "From", UrlPathSegment },
                { "To", vm.UrlPathSegment }
            });
            LogInfo("Navigated", new Dictionary<string, string>
            {
                { "Navigated To", vm.UrlPathSegment },
                { "Navigated From", UrlPathSegment }
            });

            return await HostScreen.Router.Navigate.Execute(vm);
        }

        public async Task<IRoutableViewModel> NavigateToAndReset(ViewModelBase vm)
        {
            await OnNavigatingAwayAsync();

            LogInfo("Navigation Reset", new Dictionary<string, string>
            {
                { "From", UrlPathSegment },
                { "To", vm.UrlPathSegment }
            });
            LogInfo("Navigated and reset stack", new Dictionary<string, string>
            {
                { "Navigated To", vm.UrlPathSegment },
                { "Navigated From", UrlPathSegment }
            });

            DisposeNavigationStack();

            return await HostScreen.Router.NavigateAndReset.Execute(vm);
        }

        protected async Task<IRoutableViewModel> FinishCurrentStackAndNavigateHome()
        {
            await OnNavigatingAwayAsync();

            LogInfo("Navigated to Main Stack", new Dictionary<string, string>
            {
                { "From", UrlPathSegment },
                { "To", "Home" }
            });
            LogInfo("Navigated to main stack", new Dictionary<string, string>
            {
                { "Navigated To", "Home" },
                { "Navigated From", UrlPathSegment }
            });
            
            var viewModel = await HomeViewModel.CreateAsync(
                GetProjectId(), 
                ViewModelContextProvider);
            
            DisposeNavigationStack();
            
            return await HostScreen.Router.NavigateAndReset.Execute(viewModel);
        }

        protected async Task<IRoutableViewModel> NavigateBack()
        {
            await OnNavigatingAwayAsync();

            IRoutableViewModel vm = null;
            var lastDisposablePage = Application.Current?.MainPage?.Navigation?.NavigationStack?.LastOrDefault() as IDisposable;

            if (HostScreen.Router.NavigationStack.Count > 1)
            {
                vm = HostScreen.Router.NavigationStack.GetPreviousScreen();
            }

            IRoutableViewModel navigation;
            if (vm != null)
            {
                if (vm is ViewModelBase baseVm)
                {
                    baseVm.OnNavigatingBack();
                }

                var toVm = vm.UrlPathSegment;
                LogInfo("Navigated Back", new Dictionary<string, string>
                {
                    { "From", UrlPathSegment },
                    { "To", toVm }
                });
                LogInfo("Navigated back", new Dictionary<string, string>
                {
                    { "Navigated To", vm.UrlPathSegment },
                    { "Navigated From", UrlPathSegment }
                });

                lastDisposablePage?.Dispose();

                navigation = await HostScreen.Router.NavigateBack.Execute();
                return navigation;
            }
            
            //If there's nothing on the stack (other than the page we're on), and we're on the step stack,
            //return to the main stack (should be the home page)
            var viewModel = await HomeViewModel.CreateAsync(ViewModelContextProvider.GetGrandCentralStation().CurrentProjectId, ViewModelContextProvider);
            
            lastDisposablePage?.Dispose();
            
            return await HostScreen.Router.NavigateAndReset.Execute(viewModel);
        }

        [Reactive] public ReactiveCommand<Unit, IRoutableViewModel> NavigateBackCommand { get; set; }

        protected List<IDisposable> Disposables { get; } = new List<IDisposable>();

        public virtual void Dispose()
        {
            NavigateBackCommand?.Dispose();
            NavigateBackCommand = null;

            foreach (var disposable in Disposables)
            {
                disposable?.Dispose();
            }
            Disposables?.Clear();
        }

		protected void SetLoadingScreen(bool isExecuting)
		{
			IsLoading = isExecuting;
		}
        
        public void LogInfo(string name, IDictionary<string, string> properties = null)
        {
            var statsInfoList = properties ?? new Dictionary<string, string>();
            if (!statsInfoList.ContainsKey("LoggedInUserId"))
            {
                statsInfoList.Add("LoggedInUserId", GetLoggedInUserId().ToString());
            }
            if (!statsInfoList.ContainsKey("ProjectId"))
            {
                statsInfoList.Add("ProjectId", GetProjectId().ToString());
            }

            Logger.LogInfo(name, statsInfoList);
        }
        public void LogError(Exception exception, IDictionary<string, string> properties = null)
        {
            Logger.LogError(exception, properties);
        }

        public Guid GetLoggedInUserId()
        {
            return ViewModelContextProvider.GetLoggedInUser()?.Id ?? Guid.Empty;
        }

        public Guid GetProjectId()
        {
            return ViewModelContextProvider.GetGrandCentralStation()?.CurrentProjectId ?? Guid.Empty;
        }
        
        public static string GetStepName(Step step)
        {
            return step.GetName();
        }

        private Task OnNavigatingAwayAsync()
        {
            if (HostScreen?.Router?.NavigationStack?.LastOrDefault() is ViewModelBase lastViewModel)
            {
                return lastViewModel.NavigatingAwayAsync();
            }

            return Task.CompletedTask;
        }

        protected virtual Task NavigatingAwayAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual void OnNavigatingBack()
        {
        }

        private void DisposeNavigationStack()
        {
            Application.Current?.MainPage?.Navigation?.NavigationStack?
                .Where(page => page is IDisposable)
                .Cast<IDisposable>()
                .ForEach(disposable => disposable.Dispose());
        }
    }
}