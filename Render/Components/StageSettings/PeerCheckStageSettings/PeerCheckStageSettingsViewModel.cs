using System;
using System.Linq;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;

namespace Render.Components.StageSettings.PeerCheckStageSettings
{
    public enum SelectedState
    {
        Both,
        SectionListenOnly,
        PassageListenOnly
    }
    public class PeerCheckStageSettingsViewModel : StageSettingsViewModelBase
    {
        private readonly Step _checkStep;
        private readonly Step _reviseStep;

        [Reactive]
        public bool NoSelfCheck { get; set; }
        
        [Reactive]
        public bool CheckRequireSectionListen { get; set; }
        
        [Reactive]
        public bool CheckRequirePassageListen { get; set; }
        
        [Reactive]
        public bool CheckRequireNoteListen { get; set; }
        
        [Reactive]
        public SelectedState SelectedState { get; set; }
        
        public PeerCheckStageSettingsViewModel(RenderWorkflow workflow,
            Stage stage, IViewModelContextProvider viewModelContextProvider,
            Action<Stage> updateStageCard) 
            : base(workflow, stage, viewModelContextProvider, updateStageCard)
        {
            NoSelfCheck = stage.StageSettings.GetSetting(SettingType.NoSelfCheck);

            _checkStep = stage.Steps.First(x => x.RenderStepType == RenderStepTypes.PeerCheck);
            var checkDoSectionListen = _checkStep.StepSettings.GetSetting(SettingType.DoSectionReview);
            CheckRequireSectionListen = _checkStep.StepSettings.GetSetting(SettingType.RequireSectionReview);
            var checkDoPassageListen = _checkStep.StepSettings.GetSetting(SettingType.DoPassageReview);
            CheckRequirePassageListen = _checkStep.StepSettings.GetSetting(SettingType.RequirePassageReview);
            CheckRequireNoteListen = _checkStep.StepSettings.GetSetting(SettingType.RequireNoteListen);

            _reviseStep = stage.Steps.First(x => x.RenderStepType == RenderStepTypes.PeerRevise);
            TranslateRequireNoteListen = _reviseStep.StepSettings.GetSetting(SettingType.RequireNoteListen);
            TranslateDoPassageReview = _reviseStep.StepSettings.GetSetting(SettingType.DoPassageReview);
            TranslateRequirePassageReview = _reviseStep.StepSettings.GetSetting(SettingType.RequirePassageReview);
            TranslateAllowEditing = _reviseStep.StepSettings.GetSetting(SettingType.AllowEditing);
            
            SelectedState = checkDoSectionListen && checkDoPassageListen ? SelectedState.Both :
                checkDoSectionListen ? SelectedState.SectionListenOnly : SelectedState.PassageListenOnly;
            
            this.WhenAnyValue(x => x.SelectedState)
                .Skip(1)
                .Subscribe(SetRequireCheckStatus);
            
            this.WhenAnyValue(x => x.TranslateDoPassageReview)
                .Skip(1)
                .Subscribe(b =>TranslateRequirePassageReview = b);
        }

        protected override void UpdateWorkflow()
        {
            //Stage settings
            Workflow.SetStageSetting(Stage, SettingType.NoSelfCheck, NoSelfCheck);
            
            //Check step settings
            Workflow.SetStepSetting(_checkStep, SettingType.RequireNoteListen, CheckRequireNoteListen);
            
            var checkDoSectionListen = 
                SelectedState == SelectedState.SectionListenOnly || SelectedState == SelectedState.Both;
            Workflow.SetStepSetting(_checkStep, SettingType.DoSectionReview, checkDoSectionListen);
            if (checkDoSectionListen)
                Workflow.SetStepSetting(_checkStep, SettingType.RequireSectionReview, CheckRequireSectionListen);
            
            var checkDoPassageListen =
                SelectedState == SelectedState.PassageListenOnly || SelectedState == SelectedState.Both;
            Workflow.SetStepSetting(_checkStep, SettingType.DoPassageReview, checkDoPassageListen);
            if(checkDoPassageListen)
                Workflow.SetStepSetting(_checkStep, SettingType.RequirePassageReview, CheckRequirePassageListen);
            
            //Revise step settings
            Workflow.SetStepSetting(_reviseStep, SettingType.AllowEditing, TranslateAllowEditing);
            
            Workflow.SetStepSetting(_reviseStep, SettingType.RequireNoteListen, TranslateRequireNoteListen);

            Workflow.SetStepSetting(_reviseStep, SettingType.DoPassageReview, TranslateDoPassageReview);
            if(TranslateDoPassageReview)
                Workflow.SetStepSetting(_reviseStep, SettingType.RequirePassageReview, TranslateRequirePassageReview);

            base.UpdateWorkflow();
        }

        private void SetRequireCheckStatus(SelectedState selectedState)
        {
            switch (selectedState)
            {
                case SelectedState.Both:
                    CheckRequireSectionListen = true;
                    CheckRequirePassageListen = true;
                    break;
                case SelectedState.PassageListenOnly:
                    CheckRequireSectionListen = false;
                    CheckRequirePassageListen = true;
                    break;
                case SelectedState.SectionListenOnly:
                    CheckRequireSectionListen = true;
                    CheckRequirePassageListen = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}