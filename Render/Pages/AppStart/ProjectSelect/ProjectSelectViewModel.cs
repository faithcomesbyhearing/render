using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.LocalOnlyData;
using Render.Models.Users;
using Render.Pages.AppStart.ProjectDownload;
using Render.Pages.AppStart.ProjectList;
using Render.Resources;
using Render.Resources.Localization;
using Render.TempFromVessel.Project;
using Render.Utilities;

namespace Render.Pages.AppStart.ProjectSelect
{
    public class ProjectSelectViewModel : PageViewModelBase
    {
        [Reactive]
        public ProjectListViewModel ProjectListViewModel { get; private set; }
        
        [Reactive]
        public ProjectDownloadViewModel AddProjectViewModel { get; private set; }
        
        public bool IsRenderUser { get; private set; }

        public ReactiveCommand<Unit, Unit> OnSelectedProjectList { get; }

        public ReactiveCommand<Unit, Unit> OnSelectedAddProject { get; }

        public ReactiveCommand<Unit, Unit> AddProjectFromComputerCommand { get; private set; }
        
        [Reactive] public bool ShowProjectListPanel { get; private set; }
        
        public static async Task<ProjectSelectViewModel> CreateAsync(IViewModelContextProvider contextProvider)
        {
            try
            {
                await ResetWorkForDeletedProject(contextProvider);

                var viewModel = new ProjectSelectViewModel(contextProvider);
                await viewModel.SetViewModels();
                return viewModel;
            }
            catch (Exception e)
            {
                RenderLogger.LogError(e);
                throw;
            }
        }

        private static async Task ResetWorkForDeletedProject(IViewModelContextProvider contextProvider)
        {
            var projectRepository = contextProvider.GetPersistence<Project>();
            var grandCentralStation = contextProvider.GetGrandCentralStation();
            var project = await projectRepository.GetAsync(grandCentralStation.CurrentProjectId);
            if (project != null && project.IsDeleted)
            {
                grandCentralStation.ResetWorkForUser();
            }
        }

        /// <summary>
        /// This constructor is private. Always use the static CreateAsync() method to get this viewmodel, so that the
        /// async initialization of the viewmodel can occur before you navigate.
        /// </summary>
        private ProjectSelectViewModel(IViewModelContextProvider viewModelContextProvider)
            : base("ProjectSelect", viewModelContextProvider, AppResources.SelectAProject, secondPageName: AppResources.ProjectList)
        {
            TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(Icon.ProjectList, ResourceExtensions.GetColor("SecondaryText"), 35)?.Glyph;
            IsRenderUser = viewModelContextProvider.GetLoggedInUser().UserType == UserType.Render;
            
            OnSelectedProjectList = ReactiveCommand.Create(OnSelectProjectList);
            OnSelectedAddProject = ReactiveCommand.Create(OnSelectAddProject);
            
            AddProjectFromComputerCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await AddProjectViewModel.AddFromComputerViewModel.OpenFileAndStartImport();
            });
            
            Disposables.Add(this.WhenAnyValue(x => x.ShowProjectListPanel)
                .Skip(1)
                .Subscribe(x =>
                {
                    if (ProjectListViewModel == null || AddProjectViewModel == null) return;

                    ProjectListViewModel.IsScreenSelected = x;
                    AddProjectViewModel.IsScreenSelected = !x;
                }));

            Disposables.Add(this.WhenAnyValue(x => x.ProjectListViewModel.OnOffloaded)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    if (AddProjectViewModel == null) return;
                    AddProjectViewModel.OnOffloaded = x;
                }));

            Disposables.Add(TitleBarViewModel.TitleBarMenuViewModel.NavigationItems.Observable
                .Filter(item => item?.Command != null)
                .MergeMany(item => item.Command.IsExecuting)
                .Subscribe(SetLoading));
            
            Disposables.Add(TitleBarViewModel.NavigationItems.Observable
                .MergeMany(item => item.IsExecuting)
                .Subscribe(isExecuting => IsLoading = isExecuting));
        }

        private void SetLoading(bool isExecuting)
        {
            IsLoading = isExecuting;
        }
        
        private async Task SetViewModels()
        {
            ProjectListViewModel = await ProjectListViewModel.CreateAsync(ViewModelContextProvider);
            AddProjectViewModel = await ProjectDownloadViewModel.CreateAsync(ViewModelContextProvider, RefreshProjectSelectCardViewList);
            Disposables.Add(ProjectListViewModel.ProjectList
                .ToObservableChangeSet()
                .MergeMany(item => item.NavigateToProjectCommand.IsExecuting)
                .Subscribe(isExecuting =>
                {
                    IsLoading = isExecuting;
                }));
            if (!ProjectListViewModel.ProjectList.Any())
            {
                OnSelectAddProject();
            }
            else
            {
                OnSelectProjectList();
            }
        }

        private void OnSelectProjectList()
        {
            ShowProjectListPanel = true;
        }

        private void OnSelectAddProject()
        {
            ShowProjectListPanel = false;
        }

        private async Task RefreshProjectSelectCardViewList(LocalProject project)
        {
            await ProjectListViewModel.GetAllProjectsForUser(project);
        }

        public override void Dispose()
        {
            ProjectListViewModel?.Dispose();
            AddProjectViewModel?.Dispose();

            base.Dispose();
        }
    }
}