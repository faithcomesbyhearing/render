using System;
using System.Linq;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;

namespace Render.Components.StageSettings.CommunityTestStageSettings
{
    public enum RetellQuestionResponseSettings
    {
        Both,
        Retell,
        QuestionAndResponse
    }

    public class CommunityTestStageSettingsViewModel : StageSettingsViewModelBase
    {
        private readonly Step _reviseStep;
        private readonly Step _reviewStep;
        private readonly Step _responseSetupStep;

        private readonly bool _initialAssignToTranslator;

        [Reactive]
        public bool AssignToTranslator { get; set; }
        
        [Reactive]
        public bool RetellRequireSectionListen { get; set; } // Section Listen Guidance
        
        [Reactive]
        public bool RetellRequirePassageListen { get; set; } // Passage Listen Guidance
        
        [Reactive]
        public bool ResponseRequireContextListen { get; set; } // Question Context Listen Guidance
        
        [Reactive]
        public bool ResponseRequireRecordResponse { get; set; } // Include Response Guidance

        [Reactive]
        public RetellQuestionResponseSettings SelectedState { get; set; }
        
        
        [Reactive]
        public bool ReviseAllowEditing { get; set; } // Allow Editing
        
        [Reactive]
        public bool ReviseRequireSectionListen { get; set; } // Section Listen Guidance

        [Reactive]
        public bool ReviseRequireCommunityFeedback { get; set; } // Community Feedback Guidance


        public CommunityTestStageSettingsViewModel(RenderWorkflow workflow,
            Stage stage,
            IViewModelContextProvider viewModelContextProvider,
            Action<Stage> updateStageCard) 
            : base(workflow, stage, viewModelContextProvider, updateStageCard)
        {
            _reviseStep = stage.Steps.First(step => step.RenderStepType == RenderStepTypes.CommunityRevise);
            _reviewStep = stage.Steps.First(step => step.RenderStepType == RenderStepTypes.CommunityTest);
            _responseSetupStep = stage.Steps.First(step => step.RenderStepType == RenderStepTypes.CommunitySetup);

            AssignToTranslator = stage.StageSettings.GetSetting(SettingType.AssignToTranslator);
            _initialAssignToTranslator = AssignToTranslator;

            // Review step
            RetellRequireSectionListen = _reviewStep.StepSettings.GetSetting(SettingType.RequireSectionListen);
            
            RetellRequirePassageListen = _reviewStep.StepSettings.GetSetting(SettingType.RequirePassageListen);
            ResponseRequireContextListen = _reviewStep.StepSettings.GetSetting(SettingType.RequireQuestionContextListen);
            ResponseRequireRecordResponse = _reviewStep.StepSettings.GetSetting(SettingType.RequireRecordResponse);

            SelectedState = Utilities.Utilities.GetRetellQuestionResponseSettingFrom(_reviewStep);
            
            // Revise step
            ReviseAllowEditing = _reviseStep.StepSettings.GetSetting(SettingType.AllowEditing);
            ReviseRequireSectionListen = _reviseStep.StepSettings.GetSetting(SettingType.RequireSectionListen);
            ReviseRequireCommunityFeedback = _reviseStep.StepSettings.GetSetting(SettingType.RequireCommunityFeedback);
            TranslateDoPassageReview = _reviseStep.StepSettings.GetSetting(SettingType.DoPassageReview);
            TranslateRequirePassageReview = _reviseStep.StepSettings.GetSetting(SettingType.RequirePassageReview);
            
            Disposables.Add(this
                .WhenAnyValue(x => x.SelectedState)
                .Skip(1)
                .Subscribe(SetRequireCheckStatus));
        }

        protected override void UpdateWorkflow()
        {
            //Stage settings
            Workflow.SetStageSetting(Stage, SettingType.AssignToTranslator, AssignToTranslator);

            Workflow.SetStepSetting(_reviewStep, SettingType.RequireSectionListen, RetellRequireSectionListen);

            //Retell settings
            Workflow.SetStepSetting(
                step: _reviewStep, settingType: SettingType.DoCommunityRetell, 
                value: SelectedState is RetellQuestionResponseSettings.Both || SelectedState is RetellQuestionResponseSettings.Retell);

            Workflow.SetStepSetting(_reviewStep, SettingType.RequirePassageListen, RetellRequirePassageListen);

            //Response settings
            Workflow.SetStepSetting(
                step: _responseSetupStep, settingType: SettingType.IsActive, 
                value: SelectedState is RetellQuestionResponseSettings.Both || SelectedState is RetellQuestionResponseSettings.QuestionAndResponse);

            Workflow.SetStepSetting(
                step: _reviewStep, settingType: SettingType.DoCommunityResponse, 
                value: SelectedState is RetellQuestionResponseSettings.Both || SelectedState is RetellQuestionResponseSettings.QuestionAndResponse);

            Workflow.SetStepSetting(_reviewStep, SettingType.RequireQuestionContextListen, ResponseRequireContextListen);
            Workflow.SetStepSetting(_reviewStep, SettingType.RequireRecordResponse, ResponseRequireRecordResponse);
            
            //Revise settings
            Workflow.SetStepSetting(
                step: _reviseStep, settingType: SettingType.DoCommunityRetell, 
                value: SelectedState is RetellQuestionResponseSettings.Both || SelectedState is RetellQuestionResponseSettings.Retell);

            Workflow.SetStepSetting(
                step: _reviseStep, settingType: SettingType.DoCommunityResponse, 
                value: SelectedState is RetellQuestionResponseSettings.Both || SelectedState is RetellQuestionResponseSettings.QuestionAndResponse);

            Workflow.SetStepSetting(_reviseStep, SettingType.AllowEditing, ReviseAllowEditing);
            Workflow.SetStepSetting(_reviseStep, SettingType.RequireSectionListen, ReviseRequireSectionListen);
            Workflow.SetStepSetting(_reviseStep, SettingType.RequireCommunityFeedback, ReviseRequireCommunityFeedback);

            Workflow.SetStepSetting(_reviseStep, SettingType.DoPassageReview, TranslateDoPassageReview);
            if (TranslateDoPassageReview)
            {
                Workflow.SetStepSetting(_reviseStep, SettingType.RequirePassageReview, TranslateRequirePassageReview);
            }

            if  (AssignToTranslator && !_initialAssignToTranslator)
            {
                // we clean translation assignments for this stage
                Workflow.AssignCommunityTestTranslationTeam(Stage.Id);
            }
            if (_initialAssignToTranslator && !AssignToTranslator)
            {
                // we clean translation assignments for this stage
                Workflow.CleanCommunityTestTranslationTeam(Stage.Id);
            }

            base.UpdateWorkflow();
        }

        private void SetRequireCheckStatus(RetellQuestionResponseSettings selectedState)
        {
            switch (selectedState)
            {
                case RetellQuestionResponseSettings.Both:
                    RetellRequirePassageListen = true;
                    ResponseRequireContextListen = true;
                    ResponseRequireRecordResponse = true;
                    break;
                case RetellQuestionResponseSettings.Retell:
                    RetellRequirePassageListen = true;
                    ResponseRequireContextListen = false;
                    ResponseRequireRecordResponse = false;
                    break;
                case RetellQuestionResponseSettings.QuestionAndResponse:
                    RetellRequirePassageListen = false;
                    ResponseRequireContextListen = true;
                    ResponseRequireRecordResponse = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}