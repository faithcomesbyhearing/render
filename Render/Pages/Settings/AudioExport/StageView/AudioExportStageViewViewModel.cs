using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Sections;
using Render.Models.Workflow;

namespace Render.Pages.Settings.AudioExport.StageView
{
    public class AudioExportStageViewViewModel : ViewModelBase
    {
        public readonly DynamicDataWrapper<StageViewStageViewModel> Stages = new
            DynamicDataWrapper<StageViewStageViewModel>();

        public static async Task<AudioExportStageViewViewModel> CreateAsync(
            IViewModelContextProvider viewModelContextProvider,
            List<SectionToExport> sections)
        {
            var grandCentralStation = viewModelContextProvider.GetGrandCentralStation();
            var workflow = grandCentralStation.ProjectWorkflow;
            var stages = workflow.GetAllStages();
            var approvalStage = stages.SingleOrDefault(x => x.StageType == StageTypes.ConsultantApproval);
            var workflowStatusRepository = viewModelContextProvider.GetPersistence<WorkflowStatus>();
            //Sections don't get another snapshot when they get approved, so we need to search for them explicitly
            var approvedSectionsToExport = new List<SectionToExport>();
            if (approvalStage != null)
            {
                var projectWorkflowStatusObjects = (await workflowStatusRepository
                        .QueryOnFieldAsync("ProjectId", workflow.ProjectId.ToString(), 0) ?? new List<WorkflowStatus>())
                    .Where(x => x.IsCompleted);
                var approvedSectionIds = projectWorkflowStatusObjects
                    .Where(x => x.CurrentStageId == approvalStage.Id && x.IsCompleted)
                    .Select(x => x.ParentSectionId);
                approvedSectionsToExport =
                    sections.Where(x => approvedSectionIds.Contains(x.Section.Id)).ToList();
            }

            var vm = new AudioExportStageViewViewModel(viewModelContextProvider, sections, approvedSectionsToExport);
            return vm;
        }

        private AudioExportStageViewViewModel(IViewModelContextProvider viewModelContextProvider,
            List<SectionToExport> sections, List<SectionToExport> approvedSectionsToExport)
            : base("AudioExportStageView", viewModelContextProvider)
        {
            var grandCentralStation = viewModelContextProvider.GetGrandCentralStation();
            var workflow = grandCentralStation.ProjectWorkflow;
            var stages = workflow.GetAllStages();
            var approvalStage = stages.SingleOrDefault(x => x.StageType == StageTypes.ConsultantApproval);
            //Build the views for the other stages, filtering for sections we've already determined belong in the
            //approval stage, even though their last snapshot will be somewhere else
            foreach (var stage in stages.Where(x => x.Id != approvalStage?.Id))
            {
                var stageViewStageViewModel = new StageViewStageViewModel(viewModelContextProvider, stage,
                    sections.Where(x => x.HasSnapshot && x.Snapshot.StageId == stage.Id
                                                      && !approvedSectionsToExport.Contains(x)).ToList());
                Stages.Add(stageViewStageViewModel);
            }

            //Add the approval stage last
            var approvalStageViewStageViewModel = new StageViewStageViewModel(viewModelContextProvider, approvalStage,
                approvedSectionsToExport);
            Stages.Add(approvalStageViewStageViewModel);
        }
    }
}