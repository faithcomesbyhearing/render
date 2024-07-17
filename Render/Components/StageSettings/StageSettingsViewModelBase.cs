using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.WorkflowRepositories;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;

namespace Render.Components.StageSettings
{
    public class StageSettingsViewModelBase : ViewModelBase
    {
        protected readonly Stage Stage;
        protected RenderWorkflow Workflow;
   
        private readonly IWorkflowRepository _workflowPersistence;
        
        public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }

        [Reactive] public bool TranslateDoSectionListen { get; set; }
        [Reactive] public bool TranslateRequireSectionListen { get; set; }
        [Reactive] public bool TranslateDoPassageListen { get; set; }
        [Reactive] public bool TranslateRequirePassageListen { get; set; }
        [Reactive] public bool TranslateDoSectionReview { get; set; }
        [Reactive] public bool TranslateRequireSectionReview { get; set; }
        [Reactive] public bool TranslateRequireNoteListen { get; set; }
        [Reactive] public bool TranslateDoPassageReview { get; set; }
        [Reactive] public bool TranslateRequirePassageReview { get; set; }
        [Reactive] public bool TranslateAllowEditing { get; set; }
        
        [Reactive] public string StageName { get; set; }
        private Action<Stage> UpdateStageCard;
        
        public string PlusIcon;
        public string MinusIcon;

        protected StageSettingsViewModelBase(RenderWorkflow renderWorkflow, Stage stage,
            IViewModelContextProvider viewModelContextProvider, Action<Stage> updateStageCard)
        :base("StageSettings", viewModelContextProvider)
        {
            var color = (ColorReference)ResourceExtensions.GetResourceValue("TertiaryText") ?? new ColorReference();
            PlusIcon = (IconExtensions.BuildFontImageSource(Icon.Plus, color.Color, 10) ?? new FontImageSource()).Glyph;
            MinusIcon = (IconExtensions.BuildFontImageSource(Icon.Minus, color.Color, 5) ?? new FontImageSource()).Glyph;
            Workflow = renderWorkflow;
            Stage = stage;
            StageName = Stage.Name;
            _workflowPersistence = viewModelContextProvider.GetWorkflowRepository();
            UpdateStageCard = updateStageCard;
            
            Disposables.Add(this.WhenAnyValue(x => x.TranslateDoSectionListen)
                .Skip(1)
                .Subscribe(b => TranslateRequireSectionListen = b));
            Disposables.Add(this.WhenAnyValue(x => x.TranslateDoPassageListen)
                .Skip(1)
                .Subscribe(b => TranslateRequirePassageListen = b));
            Disposables.Add(this.WhenAnyValue(x => x.TranslateDoPassageReview)
                .Skip(1)
                .Subscribe(b => TranslateRequirePassageReview = b));
            Disposables.Add(this.WhenAnyValue(x => x.TranslateDoSectionReview)
                .Skip(1)
                .Subscribe(b => TranslateRequireSectionReview = b));
            
            ConfirmCommand = ReactiveCommand.CreateFromTask(ConfirmAsync);
        }

        private async Task SaveWorkflowAsync()
        {
            LogInfo("Workflow Saved", new Dictionary<string, string>
            {
                {"WorkflowId", Workflow.Id.ToString()},
                {"ProjectId", Workflow.ProjectId.ToString()}
            });
            await _workflowPersistence.SaveWorkflowAsync(Workflow);
            var grandCentralStation = ViewModelContextProvider.GetGrandCentralStation();
            grandCentralStation.UpdateWorkflow(Workflow);
        }

        protected virtual void UpdateWorkflow()
        {
            Stage.SetName(StageName);
            LogInfo("Workflow Updated", new Dictionary<string, string>
            {
                {"WorkflowId", Workflow.Id.ToString()},
                {"ProjectId", Workflow.ProjectId.ToString()}
            });
        }
        
        public async Task ConfirmAsync()
        {
            UpdateWorkflow();
            await SaveWorkflowAsync();
            UpdateStageCard.Invoke(Stage);
        }
        
        public async Task<DialogResult> ConfirmStepDeactivationAsync()
        {
            var modalService = ViewModelContextProvider.GetModalService();

            return await modalService.ConfirmationModal(Icon.PopUpWarning,
                AppResources.SectionsMovingWarning, AppResources.DeactivateStepWarning, AppResources.Cancel,
                AppResources.Confirm);
        }

        public async Task Restore()
        {
            var workflowRepository = ViewModelContextProvider.GetWorkflowRepository();
            var currentWorkflow = await workflowRepository.GetWorkflowForProjectIdAsync(Workflow.ProjectId);
            var currentStage = currentWorkflow.GetAllStages().Single(s => s.Id == Stage.Id);
            Stage.StageSettings = currentStage.StageSettings;
            foreach (var step in Stage.Steps)
            {
                var currentStep = currentStage.Steps.Single(s => s.Id == step.Id);
                step.StepSettings = currentStep.StepSettings;
            }
        }

        public override void Dispose()
        {
            UpdateStageCard = null;

            base.Dispose();
        }
    }
}