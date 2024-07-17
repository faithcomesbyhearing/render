using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Workflow.Stage;

namespace Render.Pages.Settings.AudioExport.StageView
{
    public class StageViewStageViewModel : ViewModelBase
    {
        public ObservableCollection<SectionToExport> SectionCards { get; private set; }
        public FontImageSource Icon { get; }
        [Reactive] public bool Expand { get; set; }
        public Stage Stage { get; }
        
        public readonly ReactiveCommand<Unit, Unit> ToggleExpandCommand;

        public StageViewStageViewModel(IViewModelContextProvider viewModelContextProvider, Stage stage, List<SectionToExport> sections)
            : base("StageViewStage", viewModelContextProvider)
        {
            Stage = stage;
            Icon = IconMapper.GetStageIconForExportPage(stage.StageType);
            SectionCards = new ObservableCollection<SectionToExport>(sections.OrderBy(x => x.Section.Number));
            if (sections.Count == 0)
            {
                var emptySection = new SectionToExport(null, null, true);
                SectionCards.Add(emptySection);
            }
            
            ToggleExpandCommand = ReactiveCommand.Create(() => { Expand = !Expand; });
        }

		public override void Dispose()
		{
			foreach (var sectionCardViewModel in SectionCards)
			{
				sectionCardViewModel.Dispose();
			}
			ToggleExpandCommand?.Dispose();
			base.Dispose();
		}
	}
}