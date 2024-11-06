using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.AddFromComputer;
using Render.Kernel;
using Render.Models.LocalOnlyData;
using Render.Repositories.Kernel;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;
using Render.Services.UserServices;
using Render.TempFromVessel.Project;

namespace Render.Pages.AppStart.ProjectDownload
{
    public class ProjectDownloadViewModel : PageViewModelBase
    {
        public AddFromComputerViewModel AddFromComputerViewModel { get; }
        
        private readonly IDataPersistence<Project> _projectRepository;
        private readonly IUserMembershipService _userMembershipService;

        private readonly SourceCache<ProjectDownloadCardViewModel, Guid> _sourceCache = new (x => x.Project.Id);

        private readonly ReadOnlyObservableCollection<ProjectDownloadCardViewModel> _projectCards;
        public ReadOnlyObservableCollection<ProjectDownloadCardViewModel> ProjectCards => _projectCards;

        public bool HasProjectsToDownload 
        {
            get => _sourceCache.Items.Any(p => p.DownloadState != DownloadState.Finished); 
        }

        [Reactive] public bool ShowSelect { get; set; }

        [Reactive] public bool IsScreenSelected { get; set; }

        [Reactive] public bool OnOffloaded { get; set; }

        [Reactive] public string SearchString { get; private set; } = "";

        private readonly Func<LocalProject, Task> _refreshProjectSelectCardViewList;

        public static async Task<ProjectDownloadViewModel> CreateAsync(IViewModelContextProvider contextProvider,
			Func<LocalProject, Task> refreshProjectSelectCardViewList)
        {
            var viewModel = new ProjectDownloadViewModel(contextProvider, refreshProjectSelectCardViewList);
            viewModel.IsLoading = true;

            await viewModel.InitProjectCards();

            viewModel.IsLoading = false;
            return viewModel;
        }

        /// <summary>
        /// This constructor is private. Always use the static CreateAsync() method to get this viewmodel, so that the
        /// async initialization of the viewmodel can occur before you navigate.
        /// </summary>
        private ProjectDownloadViewModel(IViewModelContextProvider viewModelContextProvider, Func<LocalProject, Task> refreshProjectSelectCardViewList)
            : base("ProjectDownload", viewModelContextProvider, AppResources.AddProject)
        {
            _refreshProjectSelectCardViewList = refreshProjectSelectCardViewList;
            var color = (ColorReference)ResourceExtensions.GetResourceValue("SecondaryText");
            if (color != null)
            {
                TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(Icon.AddProject, color.Color, 35)?.Glyph;
            }

            _projectRepository = viewModelContextProvider.GetPersistence<Project>();
            _userMembershipService = viewModelContextProvider.GetUserMembershipService();
            
            AddFromComputerViewModel = new AddFromComputerViewModel(viewModelContextProvider, AppResources.Return, actionOnDownloadCompleted: RefreshProjectsCollection);
            
            Disposables.Add(_sourceCache.Connect()
                .AutoRefreshOnObservable(x => this.WhenAnyValue(s => s.SearchString), TimeSpan.FromMilliseconds(10))
                .Filter(x => x.Project.Name.ToLowerInvariant().Contains(SearchString.ToLowerInvariant()))
                .Sort(SortExpressionComparer<ProjectDownloadCardViewModel>.Ascending(x => x.Project.Name))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _projectCards)
                .Subscribe());

            Disposables.Add(this.WhenAnyValue(x => x.IsScreenSelected)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async selected =>
                {
                    if (selected)
                    {
                        IsLoading = true;
                        await RefreshProjectsCollection();
                        IsLoading = false;
                    }
                }));

            Disposables.Add(this.WhenAnyValue(x => x.OnOffloaded)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async offloaded =>
                {
                    if (offloaded)
                    {
                        IsLoading = true;
                        await RefreshProjectsCollection();
                        IsLoading = false;
                    }
                }));
        }

        private async Task InitProjectCards()
        {
            var downloadedProjects = await ViewModelContextProvider.GetLocalProjectsRepository().GetLocalProjectsForMachine();
            var projects = await GetAllProjectsForUserAsync(downloadedProjects);

            foreach (var project in projects)
            {
                _sourceCache.AddOrUpdate(project);
            }
        }

        private async Task<List<ProjectDownloadCardViewModel>> GetAllProjectsForUserAsync(LocalProjects localProjects)
        {
            try
            {
                var result = new List<ProjectDownloadCardViewModel>();

                var loggedInUser = ViewModelContextProvider.GetLoggedInUser();
                if (loggedInUser == null) return result;

				var allProjects = (await _projectRepository.GetAllOfTypeAsync())
	                .Where(x => !x.IsDeleted && x.HasDeployedScopes && x.IsBetaTester);

				foreach (var project in allProjects)
                {
                    if (_userMembershipService.HasExplicitPermissionForProject(loggedInUser, project.Id))
                    {
                        var state = localProjects.GetState(project.Id);

                        if (state is DownloadState.NotStarted or DownloadState.Downloading or DownloadState.FinishedPartially)
                        {
                            var projectExists = _sourceCache.Items.FirstOrDefault(x => x.Project.Id == project.Id);
                            if (projectExists is not null) continue;

                            var vm = new ProjectDownloadCardViewModel(project, ViewModelContextProvider, state, _refreshProjectSelectCardViewList);
                            result.Add(vm);
                        }
                        
                        RemoveDuplicatedCards(localProjects, project, state);
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                //TODO: Error messaging PBI#2950
                LogError(e);
                throw;
            }
        }

        private void RemoveDuplicatedCards(LocalProjects localProjects, Project project, DownloadState state)
        {
            if (_sourceCache.Items.Any(x => x.Project.ProjectId == localProjects.GetProject(project.Id).ProjectId)
                && state == DownloadState.Finished)
            {
                _sourceCache.Remove(project.ProjectId);
            }
        }
        
        private async Task RefreshProjectsCollection()
        {
            var projects = await Task.Run(async () =>
            {
                var localProjects = await ViewModelContextProvider.GetLocalProjectsRepository()
                    .GetLocalProjectsForMachine();
                return await GetAllProjectsForUserAsync(localProjects);
            });
            
            var alreadyDownloadedProjectIds = _projectCards
                .Where(p => p.DownloadState == DownloadState.Finished)
                .Select(p => p.Project.Id).ToList();

            _sourceCache.Edit(sourceCache => 
            {
                sourceCache.RemoveKeys(alreadyDownloadedProjectIds);

                foreach (var project in projects)
                {
                   _sourceCache.AddOrUpdate(project);
                }
            });
        }
    }
}