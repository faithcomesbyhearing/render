using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Audio;
using Render.Models.Project;
using Render.Pages.Settings.AudioExport.AllSectionsView;
using Render.Pages.Settings.AudioExport.StageView;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;
using Render.TempFromVessel.Project;

namespace Render.Pages.Settings.AudioExport
{
	public enum ExportingStatus
	{
		Exporting,
		Completed,
		None
	}

	public class AudioExportPageViewModel : PageViewModelBase
	{
		public readonly List<string> SortOptions = new List<string>
			{ AppResources.LastCompletedStage, AppResources.AllSections };

		[Reactive] public string SelectedSortOption { get; set; }
		[Reactive] public ExportingStatus Status { get; private set; } = ExportingStatus.None;
		[Reactive] public bool EnableExportButton { get; set; }
		[Reactive] public double ExportPercent { get; set; }
		[Reactive] public string ExportedString { get; set; }

		public AudioExportStageViewViewModel StageView { get; private set; }
		public AllSectionsViewViewModel AllSectionsView { get; private set; }

		private SourceCache<SectionToExport, Guid> _sectionSource = new SourceCache<SectionToExport, Guid>
			(x => x.Section.Id);

		private ReadOnlyObservableCollection<SectionToExport> _sections;
		private ReadOnlyObservableCollection<SectionToExport> Sections => _sections;

		public readonly ReactiveCommand<Unit, Unit> ExportCommand;

		public static async Task<AudioExportPageViewModel> CreateAsync(
			IViewModelContextProvider viewModelContextProvider,
			Guid projectId)
		{
			var sectionRepository = viewModelContextProvider.GetSectionRepository();
			var snapshotRepository = viewModelContextProvider.GetSnapshotRepository();
			var sections = await sectionRepository.GetSectionsForProjectAsync(projectId);
			var sectionsToExport = new List<SectionToExport>();
			foreach (var section in sections)
			{
				var snapShotList = await snapshotRepository.GetPermanentSnapshotsForSectionAsync(section.Id);
				var latestSnapshot = snapShotList.LastOrDefault();
				var sectionToExport = new SectionToExport(section, latestSnapshot);
				sectionsToExport.Add(sectionToExport);
			}

			var project = await viewModelContextProvider.GetPersistence<Project>().GetAsync(projectId);
			var stageView = await AudioExportStageViewViewModel.CreateAsync(viewModelContextProvider, sectionsToExport);
			var vm = new AudioExportPageViewModel(viewModelContextProvider, sectionsToExport, stageView, project.Name ?? "");
			return vm;
		}

		private AudioExportPageViewModel(IViewModelContextProvider viewModelContextProvider,
			List<SectionToExport> sectionsToExport, AudioExportStageViewViewModel stageView, string pageName)
			: base("AudioExportPage", viewModelContextProvider, AppResources.ExportAudio, secondPageName: pageName)
		{
			var color = (ColorReference)ResourceExtensions.GetResourceValue("SecondaryText");

			if (color != null)
			{
				TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(Icon.ExportAudio, color.Color, 35)?.Glyph;
			}

			_sectionSource.Connect()
				.WhenPropertyChanged(x => x.Selected)
				.Subscribe(b =>
				{
					if (b.Value)
					{
						EnableExportButton = b.Value;
					}
					else
					{
						EnableExportButton = _sections?.Any(x => x.Selected) == true;
					}

					HideProgressBar();
				});
			_sectionSource.Connect()
				.Bind(out _sections)
				.Subscribe();

			_sectionSource.AddOrUpdate(sectionsToExport);
			StageView = stageView;
			AllSectionsView = new AllSectionsViewViewModel(viewModelContextProvider, Sections);
			var canExecuteExportAsync = this.WhenAnyValue(x => x.EnableExportButton);
			ExportCommand = ReactiveCommand.CreateFromTask(ExportAsync, canExecuteExportAsync);
			SelectedSortOption = SortOptions.First();
			Disposables.Add(TitleBarViewModel.TitleBarMenuViewModel.NavigationItems.Items
				.ToObservableChangeSet()
				.ObserveOn(RxApp.MainThreadScheduler)
				.MergeMany(item => item.Command.IsExecuting)
				.Subscribe(SetLoadingScreen));
			Disposables.Add(TitleBarViewModel.NavigationItems.Items
				.ToObservableChangeSet()
				.MergeMany(item => item.IsExecuting)
				.Subscribe(isExecuting => { IsLoading = isExecuting; }));
			Disposables.Add(this.WhenAnyValue(x => x.SelectedSortOption)
				.Subscribe(x => { HideProgressBar(); }));

			Disposables.Add(ExportCommand.ThrownExceptions.Subscribe(async exception =>
			{
				var result = await viewModelContextProvider.GetModalService().ConfirmationModal(Icon.TypeWarning, AppResources.Error, AppResources.DownloadFailed,
					AppResources.Cancel, AppResources.TryAgain);
				if (result == Kernel.WrappersAndExtensions.DialogResult.Ok)
				{
					await ExportAsync();
				}
			}));
		}

		private async Task ExportAsync()
		{
			ExportPercent = 0.0;
			var totalToExport = Sections.Count(x => x.Selected);
			ExportedString = string.Format(AppResources.Exporting, 0, totalToExport);
			var downloadService = ViewModelContextProvider.GetDownloadService();
			var draftRepository = ViewModelContextProvider.GetDraftRepository();
			var renderProjectRepository = ViewModelContextProvider.GetPersistence<RenderProject>();
			var projectRepository = ViewModelContextProvider.GetPersistence<Project>();
			var projectId = Sections.First().Section.ProjectId;
			var renderProject = await renderProjectRepository.QueryOnFieldAsync("ProjectId", projectId.ToString());


			var permission = await ViewModelContextProvider.GetEssentials().AskForFileAccessPermissions();

			if (permission)
			{
				var numberExported = 0.0;
				ExportPercent = 0.0;
				var result = await downloadService.ChooseFilePathAsync();
				if (result != null)
				{
					Status = ExportingStatus.Exporting;
					foreach (var section in Sections.Where(x => x.Selected))
					{
						var snapshot = await ViewModelContextProvider.GetSnapshotRepository().GetPassageDraftsForSnapshot(section.Snapshot);

						var projectName = renderProject.GetLanguageName();
						if (string.IsNullOrWhiteSpace(projectName))
						{
							var project = await projectRepository.QueryOnFieldAsync("Id", projectId.ToString());
							projectName = project.Name;
						}

						var fileName = ViewModelContextProvider.GetFileNameGeneratorService()
							.GetFileNameForSnapshot(section.Section, projectName, snapshot.StageName, "wav");
						var snapshotAudio = new AudioPlayback(Guid.NewGuid(), snapshot.Passages.Select(x => x.CurrentDraftAudio));
						var tempAudioService = ViewModelContextProvider.GetTempAudioService(snapshotAudio);

						await using (var draftsSequenceAudioStream = tempAudioService.OpenAudioStream())
						{
							await downloadService.DownloadAsync(draftsSequenceAudioStream, fileName);
						}
						
						numberExported++;
						ExportPercent = numberExported / totalToExport;
						ExportedString = string.Format(AppResources.Exporting, numberExported, totalToExport);
					}
				}

				Status = ExportingStatus.Completed;
			}
		}

		private void HideProgressBar()
		{
			if (Status == ExportingStatus.Completed)
			{
				Status = ExportingStatus.None;
			}
		}

        public override void Dispose()
        {
			_sectionSource?.Dispose();
			_sectionSource = null;
			_sections = null;

			StageView?.Dispose();
			StageView = null;

			AllSectionsView?.Dispose();
			AllSectionsView = null;

            base.Dispose();
        }
    }
}