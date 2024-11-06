using Render.Kernel;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;

namespace Render.Components.StageSettings.DraftingStageSettings
{
    public class DraftingStageSettingsViewModel : StageSettingsViewModelBase
    {
        private readonly Step _draftingStep;

        public StepNameViewModel DraftingStepName { get; }

        public DraftingStageSettingsViewModel(
            RenderWorkflow workflow,
            Stage stage,
            IViewModelContextProvider viewModelContextProvider,
            Action<Stage> updateStageCard)
            : base(
                renderWorkflow: workflow,
                stage: stage,
                viewModelContextProvider: viewModelContextProvider,
                updateStageCard: updateStageCard)
        {
            _draftingStep = Stage.Steps.First();
            DraftingStepName = new StepNameViewModel(_draftingStep);

            TranslateDoSectionListen =
                _draftingStep.StepSettings.GetSetting(SettingType.DoSectionListen);
            TranslateRequireSectionListen =
                _draftingStep.StepSettings.GetSetting(SettingType.RequireSectionListen);
            TranslateDoPassageListen =
                _draftingStep.StepSettings.GetSetting(SettingType.DoPassageListen);
            TranslateRequirePassageListen =
                _draftingStep.StepSettings.GetSetting(SettingType.RequirePassageListen);
            TranslateDoSectionReview =
                _draftingStep.StepSettings.GetSetting(SettingType.DoSectionReview);
            TranslateRequireSectionReview =
                _draftingStep.StepSettings.GetSetting(SettingType.RequireSectionReview);
            TranslateDoPassageReview =
                _draftingStep.StepSettings.GetSetting(SettingType.DoPassageReview);
            TranslateRequirePassageReview =
                _draftingStep.StepSettings.GetSetting(SettingType.RequirePassageReview);
        }

        protected override void UpdateWorkflow()
        {
            DraftingStepName.UpdateEntity();

            Workflow.SetStepSetting(_draftingStep, SettingType.DoSectionListen, TranslateDoSectionListen);

            if (TranslateDoSectionListen)
            {
                Workflow.SetStepSetting(_draftingStep, SettingType.RequireSectionListen, TranslateRequireSectionListen);
            }

            Workflow.SetStepSetting(_draftingStep, SettingType.DoPassageListen, TranslateDoPassageListen);

            if (TranslateDoPassageListen)
            {
                Workflow.SetStepSetting(_draftingStep, SettingType.RequirePassageListen, TranslateRequirePassageListen);
            }

            Workflow.SetStepSetting(_draftingStep, SettingType.DoSectionReview, TranslateDoSectionReview);

            if (TranslateDoSectionReview)
            {
                Workflow.SetStepSetting(_draftingStep, SettingType.RequireSectionReview, TranslateRequireSectionReview);
            }

            Workflow.SetStepSetting(_draftingStep, SettingType.DoPassageReview, TranslateDoPassageReview);

            if (TranslateDoPassageReview)
            {
                Workflow.SetStepSetting(_draftingStep, SettingType.RequirePassageReview, TranslateRequirePassageReview);
            }

            base.UpdateWorkflow();
        }
    }
}