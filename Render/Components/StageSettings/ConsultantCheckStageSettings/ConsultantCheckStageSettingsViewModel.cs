using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;

namespace Render.Components.StageSettings.ConsultantCheckStageSettings
{
    public class ConsultantCheckStageSettingsViewModel : StageSettingsViewModelBase
    {
        private readonly Step _backTranslateStep;
        private readonly Step _backTranslate2Step;
        private readonly List<Step> _noteInterpretSteps;
        private readonly List<Step> _transcribeSteps = new();
        private readonly Step _checkStep;
        private readonly Step _reviseStep;
        private readonly Step _transcribeStep;
        private readonly Step _transcribe2Step;

        public StepNameViewModel CheckStepName { get; }
        public StepNameViewModel ReviseStepName { get; }
        public StepNameViewModel InterpretToTranslatorStepName { get; }
        public StepNameViewModel InterpretToConsultantStepName { get; }
        public StepNameViewModel Transcribe2StepName { get; }
        public StepNameViewModel TranscribeStepName { get; }
        public StepNameViewModel BackTranslate2StepName { get; }
        public StepNameViewModel BackTranslateStepName { get; }

        [Reactive] public bool AllowStepBackTranslation { get; set; }
        [Reactive] public bool Allow2StepBackTranslation { get; set; }
        [Reactive] public bool RetellStepBackTranslation { get; set; }
        [Reactive] public bool SegmentStepBackTranslation { get; set; }
        [Reactive] public bool DoStepBackTranslation { get; set; }
        [Reactive] public bool Do2StepBackTranslation { get; set; }
        [Reactive] public bool CheckRequireNoteListen { get; set; }

        #region Retell Settings

        [Reactive] public bool RetellIsActive { get; set; }
        [Reactive] public bool RetellRequireSectionListen { get; set; }
        [Reactive] public bool RetellRequirePassageListen { get; set; }
        [Reactive] public bool RetellDoPassageReview { get; set; }
        [Reactive] public bool RetellRequirePassageReview { get; set; }


        [Reactive] public bool Retell2IsActive { get; set; }
        [Reactive] public bool Retell2RequireSectionListen { get; set; }
        [Reactive] public bool Retell2RequirePassageListen { get; set; }
        [Reactive] public bool Retell2DoPassageReview { get; set; }
        [Reactive] public bool Retell2RequirePassageReview { get; set; }
        [Reactive] public bool AllowTurnOnRetell2 { get; set; }
        [Reactive] public string ConsultantLanguage { get; set; } //If two step back translation is on, this is the Immediate language for step 1.
        [Reactive] public string ConsultantLanguage2 { get; set; } //If two step back translation is on, this is the Consultant language for step 2.

        #endregion

        #region Segment Back Translate Settings

        [Reactive] public bool SegmentIsActive { get; set; }
        [Reactive] public bool SegmentRequireSectionListen { get; set; }
        [Reactive] public bool SegmentRequirePassageListen { get; set; }
        [Reactive] public bool SegmentDoPassageReview { get; set; }
        [Reactive] public bool SegmentRequirePassageReview { get; set; }


        [Reactive] public bool Segment2IsActive { get; set; }
        [Reactive] public bool Segment2RequireSectionListen { get; set; }
        [Reactive] public bool Segment2RequirePassageListen { get; set; }
        [Reactive] public bool Segment2DoPassageReview { get; set; }
        [Reactive] public bool Segment2RequirePassageReview { get; set; }

        [Reactive] public bool AllowTurnOnSegment2 { get; set; }
        [Reactive] public string SegmentConsultantLanguage { get; set; } //If two step back translation is on, this is the Immediate language for step 1.
        [Reactive] public string SegmentConsultantLanguage2 { get; set; } //If two step back translation is on, this is the Consultant language for step 2.

        #endregion

        #region Note Interpret Settings

        [Reactive] public bool NoteInterpretIsActive { get; set; }
        [Reactive] public bool RequireNoteListen { get; set; }
        [Reactive] public bool DoNoteReview { get; set; }
        [Reactive] public bool RequireNoteReview { get; set; }

        #endregion

        #region Transcribe Settings

        [Reactive] public bool DoPassageTranscribeIsActive { get; set; }
        [Reactive] public bool RequirePassageTranscribeListenIsActive { get; set; }
        [Reactive] public bool SegmentTranscribeIsActive { get; set; }
        [Reactive] public bool RequireSegmentTranscribeListenIsActive { get; set; }

        [Reactive] public bool DoPassage2TranscribeIsActive { get; set; }
        [Reactive] public bool RequirePassage2TranscribeListenIsActive { get; set; }
        [Reactive] public bool Segment2TranscribeIsActive { get; set; }
        [Reactive] public bool RequireSegment2TranscribeListenIsActive { get; set; }

        #endregion

        public ConsultantCheckStageSettingsViewModel(
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
            _checkStep = stage.Steps.First(x => x.RenderStepType == RenderStepTypes.ConsultantCheck);
            CheckRequireNoteListen = _checkStep.StepSettings.GetSetting(SettingType.RequireNoteListen);

            #region Retell Back Translate Settings

            var btMultiStep = stage.Steps.First(x => x.Order == Step.Ordering.Parallel).GetSubSteps()
                .First(x => x.Role == Roles.BackTranslate);
            _backTranslateStep = btMultiStep.GetSubSteps()
                .First(x => x.RenderStepType == RenderStepTypes.BackTranslate);
            RetellIsActive = _backTranslateStep.StepSettings.GetSetting(SettingType.DoRetellBackTranslate);
            RetellRequireSectionListen = _backTranslateStep.StepSettings.GetSetting(SettingType.RequireRetellBTSectionListen);
            RetellRequirePassageListen = _backTranslateStep.StepSettings.GetSetting(SettingType.RequireRetellBTPassageListen);
            RetellDoPassageReview = _backTranslateStep.StepSettings.GetSetting(SettingType.DoRetellBTPassageReview);
            RetellRequirePassageReview = _backTranslateStep.StepSettings.GetSetting(SettingType.RequireRetellBTPassageReview);
            ConsultantLanguage = _backTranslateStep.StepSettings.GetString(SettingType.ConsultantLanguage);
            Disposables.Add(this.WhenAnyValue(x => x.RetellIsActive)
                .Skip(1)
                .Subscribe(b =>
                {
                    RetellRequireSectionListen = b;
                    RetellRequirePassageListen = b;
                    RetellDoPassageReview = b;
                    DoPassageTranscribeIsActive = false;
                    if (!b)
                    {
                        Retell2RequireSectionListen = false;
                        Retell2RequirePassageListen = false;
                        Retell2DoPassageReview = false;
                        DoPassage2TranscribeIsActive = false;
                    }
                }));
            Disposables.Add(this.WhenAnyValue(x => x.RetellDoPassageReview)
                .Skip(1)
                .Subscribe(b => RetellRequirePassageReview = b));

            //Retell 1 Transcribe
            _transcribeStep = btMultiStep.GetSubSteps().First(x => x.Order == Step.Ordering.Parallel)
                .GetSubSteps().First(x => x.RenderStepType == RenderStepTypes.Transcribe);
            _transcribeSteps.Add(_transcribeStep);
            DoPassageTranscribeIsActive = _transcribeStep.StepSettings.GetSetting(SettingType.DoPassageTranscribe);
            RequirePassageTranscribeListenIsActive = _transcribeStep.StepSettings.GetSetting(SettingType.RequirePassageTranscribeListen);

            Disposables.Add(this.WhenAnyValue(x => x.DoPassageTranscribeIsActive)
                .Skip(1)
                .Subscribe(b => RequirePassageTranscribeListenIsActive = b));

            //Retell 2 Back Translate Settings
            _backTranslate2Step = btMultiStep.GetSubSteps().First(x => x.Order == Step.Ordering.Parallel)
                .GetSubSteps().First(x => x.GetSubSteps().Count > 0).GetSubSteps()
                .First(x => x.Role == Roles.BackTranslate2);
            Retell2IsActive = _backTranslate2Step.StepSettings.GetSetting(SettingType.DoRetellBackTranslate);
            Retell2RequireSectionListen = _backTranslate2Step.StepSettings.GetSetting(SettingType.RequireRetellBTSectionListen);
            Retell2RequirePassageListen = _backTranslate2Step.StepSettings.GetSetting(SettingType.RequireRetellBTPassageListen);
            Retell2DoPassageReview = _backTranslate2Step.StepSettings.GetSetting(SettingType.DoRetellBTPassageReview);
            Retell2RequirePassageReview = _backTranslate2Step.StepSettings.GetSetting(SettingType.RequireRetellBTPassageReview);
            ConsultantLanguage2 = _backTranslate2Step.StepSettings.GetString(SettingType.Consultant2StepLanguage);

            Disposables.Add(this.WhenAnyValue(x => x.RetellIsActive)
                .Subscribe(b => AllowTurnOnRetell2 = b));
            Disposables.Add(this.WhenAnyValue(x => x.Retell2IsActive)
                .Skip(1)
                .Subscribe(b =>
                {
                    Retell2RequireSectionListen = b;
                    Retell2RequirePassageListen = b;
                    Retell2DoPassageReview = b;
                    DoPassage2TranscribeIsActive = false;
                }));
            Disposables.Add(this.WhenAnyValue(x => x.Retell2DoPassageReview)
                .Skip(1)
                .Subscribe(b => Retell2RequirePassageReview = b));

            Disposables.Add(this.WhenAnyValue(x => x.RetellIsActive)
                .Subscribe(b =>
                {
                    if ((DoStepBackTranslation || Do2StepBackTranslation) && !b)
                        Retell2IsActive = false;
                }));

            //Retell 2 Transcribe
            _transcribe2Step = btMultiStep.GetSubSteps().First(x => x.Order == Step.Ordering.Parallel)
                .GetSubSteps().First(x => x.GetSubSteps().Count > 0).GetSubSteps()
                .First(x => x.RenderStepType == RenderStepTypes.Transcribe);
            _transcribeSteps.Add(_transcribe2Step);
            DoPassage2TranscribeIsActive = _transcribe2Step.StepSettings.GetSetting(SettingType.DoPassageTranscribe);
            RequirePassage2TranscribeListenIsActive = _transcribeStep.StepSettings.GetSetting(SettingType.RequirePassageTranscribeListen);

            Disposables.Add(this.WhenAnyValue(x => x.DoPassage2TranscribeIsActive)
                .Skip(1)
                .Subscribe(b => RequirePassage2TranscribeListenIsActive = b));

            #endregion

            #region Segment Back Translate Settings

            SegmentIsActive = _backTranslateStep.StepSettings.GetSetting(SettingType.DoSegmentBackTranslate);
            SegmentRequireSectionListen =
                _backTranslateStep.StepSettings.GetSetting(SettingType.RequireSegmentBTSectionListen);
            SegmentRequirePassageListen =
                _backTranslateStep.StepSettings.GetSetting(SettingType.RequireSegmentBTPassageListen);
            SegmentDoPassageReview = _backTranslateStep.StepSettings.GetSetting(SettingType.DoSegmentBTPassageReview);
            SegmentRequirePassageReview =
                _backTranslateStep.StepSettings.GetSetting(SettingType.RequireSegmentBTPassageReview);
            SegmentConsultantLanguage = _backTranslateStep.StepSettings.GetString(SettingType.SegmentConsultantLanguage);
            Disposables.Add(this.WhenAnyValue(x => x.SegmentIsActive)
                .Skip(1)
                .Subscribe(b =>
                {
                    SegmentRequireSectionListen = b;
                    SegmentRequirePassageListen = b;
                    SegmentDoPassageReview = b;
                    SegmentTranscribeIsActive = false;
                    if (!b)
                    {
                        Segment2RequireSectionListen = false;
                        Segment2RequirePassageListen = false;
                        Segment2DoPassageReview = false;
                        Segment2TranscribeIsActive = false;
                    }
                }));
            Disposables.Add(this.WhenAnyValue(x => x.SegmentDoPassageReview)
                .Skip(1)
                .Subscribe(b => SegmentRequirePassageReview = b));

            //Segment 1 Transcribe
            SegmentTranscribeIsActive = _transcribeStep.StepSettings.GetSetting(SettingType.DoSegmentTranscribe);
            RequireSegmentTranscribeListenIsActive = _transcribeStep.StepSettings.GetSetting(SettingType.RequireSegmentTranscribeListen);

            Disposables.Add(this.WhenAnyValue(x => x.SegmentTranscribeIsActive)
                .Skip(1)
                .Subscribe(b => RequireSegmentTranscribeListenIsActive = b));

            //Segment Back Translate 2 Settings
            var _segment2Step = btMultiStep.GetSubSteps().First(x => x.Order == Step.Ordering.Parallel).GetSubSteps()
                .First(x => x.GetSubSteps().Count > 0).GetSubSteps()
                .First(x => x.Role == Roles.BackTranslate2);
            Segment2IsActive = _segment2Step.StepSettings.GetSetting(SettingType.DoSegmentBackTranslate);
            Segment2RequireSectionListen =
                _segment2Step.StepSettings.GetSetting(SettingType.RequireSegmentBTSectionListen);
            Segment2RequirePassageListen =
                _segment2Step.StepSettings.GetSetting(SettingType.RequireSegmentBTPassageListen);
            Segment2DoPassageReview = _segment2Step.StepSettings.GetSetting(SettingType.DoSegmentBTPassageReview);
            Segment2RequirePassageReview =
                _segment2Step.StepSettings.GetSetting(SettingType.RequireSegmentBTPassageReview);
            SegmentConsultantLanguage2 = _segment2Step.StepSettings.GetString(SettingType.SegmentConsultant2StepLanguage);
            Disposables.Add(this.WhenAnyValue(x => x.SegmentIsActive)
                .Subscribe(b => AllowTurnOnSegment2 = b));
            Disposables.Add(this.WhenAnyValue(x => x.Segment2IsActive)
                .Skip(1)
                .Subscribe(b =>
                {
                    Segment2RequireSectionListen = b;
                    Segment2RequirePassageListen = b;
                    Segment2DoPassageReview = b;
                    Segment2TranscribeIsActive = false;
                }));
            Disposables.Add(this.WhenAnyValue(x => x.Segment2DoPassageReview)
                .Skip(1)
                .Subscribe(b => Segment2RequirePassageReview = b));

            Disposables.Add(this.WhenAnyValue(x => x.SegmentIsActive)
                .Subscribe(b =>
                {
                    if ((DoStepBackTranslation || Do2StepBackTranslation) && !b)
                        Segment2IsActive = false;
                }));

            //Segment 2 Transcribe
            Segment2TranscribeIsActive = _transcribe2Step.StepSettings.GetSetting(SettingType.DoSegmentTranscribe);
            RequireSegment2TranscribeListenIsActive = _transcribeStep.StepSettings.GetSetting(SettingType.RequireSegmentTranscribeListen);

            Disposables.Add(this.WhenAnyValue(x => x.Segment2TranscribeIsActive)
                .Skip(1)
                .Subscribe(b => RequireSegment2TranscribeListenIsActive = b));

            #endregion

            #region Note Interpret Settings

            //Note Interpret Settings: There is a step for "ToConsultant" and "ToTranslator", so we need to account for both
            _noteInterpretSteps = stage.Steps.Where(x => x.RenderStepType == RenderStepTypes.InterpretToConsultant ||
                                                         x.RenderStepType == RenderStepTypes.InterpretToTranslator).ToList();
            var singleInterpretStep = _noteInterpretSteps.First();
            NoteInterpretIsActive = singleInterpretStep.StepSettings.GetSetting(SettingType.IsActive);
            RequireNoteListen = singleInterpretStep.StepSettings.GetSetting(SettingType.RequireNoteListen);
            DoNoteReview = singleInterpretStep.StepSettings.GetSetting(SettingType.DoNoteReview);
            RequireNoteReview = singleInterpretStep.StepSettings.GetSetting(SettingType.RequireNoteReview);
            Disposables.Add(this.WhenAnyValue(x => x.NoteInterpretIsActive)
                .Skip(1)
                .Subscribe(b =>
                {
                    RequireNoteListen = b;
                    DoNoteReview = b;
                }));
            Disposables.Add(this.WhenAnyValue(x => x.DoNoteReview)
                .Skip(1)
                .Subscribe(b => RequireNoteReview = b));

            #endregion

            Disposables.Add(this.WhenAnyValue(x => x.RetellIsActive, x => x.SegmentIsActive,
                    x => x.DoStepBackTranslation, x => x.Do2StepBackTranslation)
                .Subscribe(args =>
                    AllowStepBackTranslation = args.Item4 || args.Item1 || args.Item2 || args.Item3));
            Disposables.Add(this.WhenAnyValue(x => x.RetellIsActive, x => x.SegmentIsActive,
                    x => x.Do2StepBackTranslation)
                .Subscribe(args =>
                    Allow2StepBackTranslation = args.Item3 || args.Item1 || args.Item2));

            RetellStepBackTranslation = RetellIsActive || RetellRequireSectionListen || RetellRequirePassageListen ||
                                        RetellDoPassageReview || RetellRequirePassageReview || DoPassageTranscribeIsActive;
            SegmentStepBackTranslation = SegmentIsActive || SegmentRequireSectionListen || SegmentRequirePassageListen ||
                                         SegmentDoPassageReview || SegmentRequirePassageReview || SegmentTranscribeIsActive;
            DoStepBackTranslation = RetellStepBackTranslation || SegmentStepBackTranslation || RetellIsActive || Retell2IsActive;
            Do2StepBackTranslation = Retell2IsActive || Segment2IsActive;

            Disposables.Add(this.WhenAnyValue(x => x.SegmentStepBackTranslation,
                    x => x.Do2StepBackTranslation,
                    x => x.DoStepBackTranslation)
                .Skip(1)
                .Subscribe(b =>
                {
                    if (b.Item3) //Include step back translation
                    {
                        if (b.Item2) //2 step back translation
                        {
                            if (b.Item1) //Include segment back translation
                            {
                                //All options should be on except transcribe
                                SegmentIsActive = true;
                                Segment2IsActive = true;
                            }
                            else
                            {
                                //All options are off
                                SegmentIsActive = false;
                                SegmentTranscribeIsActive = false;
                                Segment2IsActive = false;
                                Segment2TranscribeIsActive = false;
                            }
                        }
                        else
                        {
                            //No 2 step back translation
                            if (b.Item1)
                            {
                                //Turn on only 1st step of segment
                                SegmentIsActive = true;
                            }
                            else
                            {
                                //Turn off only 1st step of segment
                                SegmentIsActive = false;
                                SegmentTranscribeIsActive = false;
                            }

                            //Turn off 2nd step of segment
                            Segment2IsActive = false;
                            Segment2TranscribeIsActive = false;
                        }
                    }
                    else
                    {
                        //Turn off all options
                        SegmentIsActive = false;
                        SegmentTranscribeIsActive = false;
                        Segment2IsActive = false;
                        Segment2TranscribeIsActive = false;
                    }
                }));

            Disposables.Add(this.WhenAnyValue(x => x.RetellStepBackTranslation,
                    x => x.Do2StepBackTranslation,
                    x => x.DoStepBackTranslation)
                .Skip(1)
                .Subscribe(b =>
                {
                    if (b.Item3) //Include step back translation
                    {
                        if (b.Item2) //2 step back translation
                        {
                            if (b.Item1) //Include retell back translation
                            {
                                //All options should be on except transcribe
                                RetellIsActive = true;
                                Retell2IsActive = true;
                            }
                            else
                            {
                                //All options are off
                                RetellIsActive = false;
                                DoPassageTranscribeIsActive = false;
                                Retell2IsActive = false;
                                DoPassage2TranscribeIsActive = false;
                            }
                        }
                        else
                        {
                            //No 2 step back translation
                            if (b.Item1)
                            {
                                //Turn on only 1st step of retell
                                RetellIsActive = true;
                            }
                            else
                            {
                                //Turn off only 1st step of retell
                                RetellIsActive = false;
                                DoPassageTranscribeIsActive = false;
                            }

                            //Turn off 2nd step of retell
                            Retell2IsActive = false;
                            DoPassage2TranscribeIsActive = false;
                        }
                    }
                    else
                    {
                        //Turn off all options
                        RetellIsActive = false;
                        DoPassageTranscribeIsActive = false;
                        Retell2IsActive = false;
                        DoPassage2TranscribeIsActive = false;
                    }
                }));

            Disposables.Add(this.WhenAnyValue(x => x.DoStepBackTranslation)
                .Skip(1)
                .Subscribe(b =>
                {
                    RetellStepBackTranslation = b;
                    SegmentStepBackTranslation = b;
                }));

            #region Revise Settings

            _reviseStep = stage.Steps.First(x => x.RenderStepType == RenderStepTypes.ConsultantRevise);
            TranslateRequireNoteListen = _reviseStep.StepSettings.GetSetting(SettingType.RequireNoteListen);
            TranslateDoPassageReview = _reviseStep.StepSettings.GetSetting(SettingType.DoPassageReview);
            TranslateRequirePassageReview = _reviseStep.StepSettings.GetSetting(SettingType.RequirePassageReview);
            TranslateAllowEditing = _reviseStep.StepSettings.GetSetting(SettingType.AllowEditing);

            #endregion

            CheckStepName = new StepNameViewModel(_checkStep);
            ReviseStepName = new StepNameViewModel(_reviseStep);
            BackTranslateStepName = new StepNameViewModel(_backTranslateStep);
            BackTranslate2StepName = new StepNameViewModel(_backTranslate2Step);
            TranscribeStepName = new StepNameViewModel(_transcribeStep);
            Transcribe2StepName = new StepNameViewModel(_transcribe2Step);

            var interpretToConsultantStep = stage.Steps.FirstOrDefault(x => x.RenderStepType == RenderStepTypes.InterpretToConsultant);
            var interpretToTranslator = stage.Steps.FirstOrDefault(x => x.RenderStepType == RenderStepTypes.InterpretToTranslator);
            InterpretToConsultantStepName = new StepNameViewModel(interpretToConsultantStep);
            InterpretToTranslatorStepName = new StepNameViewModel(interpretToTranslator);
        }

        protected override void UpdateWorkflow()
        {
            Workflow.SetStepSetting(_checkStep, SettingType.RequireNoteListen, CheckRequireNoteListen);

            CheckStepName.UpdateEntity();
            ReviseStepName.UpdateEntity();
            InterpretToConsultantStepName.UpdateEntity();
            InterpretToTranslatorStepName.UpdateEntity();

            BackTranslateStepName.UpdateEntity();
            BackTranslate2StepName.StepName = BackTranslateStepName.StepName;
            BackTranslate2StepName.UpdateEntity();

            TranscribeStepName.UpdateEntity();
            Transcribe2StepName.StepName = TranscribeStepName.StepName;
            Transcribe2StepName.UpdateEntity();

            //turn the step off if both retell and segment are off
            bool isBackTranslateActive = RetellIsActive || SegmentIsActive;
            Workflow.SetStepSetting(_backTranslateStep, SettingType.IsActive, isBackTranslateActive);

            bool isBackTranslate2Active = Retell2IsActive || Segment2IsActive;
            Workflow.SetStepSetting(_backTranslate2Step, SettingType.IsActive, isBackTranslate2Active);

            if (!DoPassageTranscribeIsActive && !SegmentTranscribeIsActive)
            {
                Workflow.SetStepSetting(_transcribeStep, SettingType.IsActive, false);
            }
            else
            {
                Workflow.SetStepSetting(_transcribeStep, SettingType.IsActive, true);
            }

            if (!DoPassage2TranscribeIsActive && !Segment2TranscribeIsActive)
            {
                Workflow.SetStepSetting(_transcribe2Step, SettingType.IsActive, false);
            }
            else
            {
                Workflow.SetStepSetting(_transcribe2Step, SettingType.IsActive, true);
            }

            //Retell step settings
            Workflow.SetStepSetting(_backTranslateStep, SettingType.DoRetellBackTranslate, RetellIsActive);
            Workflow.SetStepSetting(_backTranslateStep, SettingType.RequireRetellBTSectionListen,
                RetellRequireSectionListen);
            Workflow.SetStepSetting(_backTranslateStep, SettingType.RequireRetellBTPassageListen,
                RetellRequirePassageListen);
            Workflow.SetStepSetting(_backTranslateStep, SettingType.DoRetellBTPassageReview, RetellDoPassageReview);
            Workflow.SetStepSetting(_backTranslateStep, SettingType.RequireRetellBTPassageReview,
                RetellRequirePassageReview);
            Workflow.SetStepSetting(_backTranslateStep, SettingType.ConsultantLanguage,
                false, ConsultantLanguage);

            //Retell 2 Step Settings
            Workflow.SetStepSetting(_backTranslate2Step, SettingType.DoRetellBackTranslate, Retell2IsActive);
            Workflow.SetStepSetting(_backTranslate2Step, SettingType.RequireRetellBTSectionListen,
                Retell2RequireSectionListen);
            Workflow.SetStepSetting(_backTranslate2Step, SettingType.RequireRetellBTPassageListen,
                Retell2RequirePassageListen);
            Workflow.SetStepSetting(_backTranslate2Step, SettingType.DoRetellBTPassageReview,
                Retell2DoPassageReview);
            Workflow.SetStepSetting(_backTranslate2Step, SettingType.RequireRetellBTPassageReview,
                Retell2RequirePassageReview);
            Workflow.SetStepSetting(_backTranslate2Step, SettingType.ConsultantLanguage,
                false, ConsultantLanguage);
            Workflow.SetStepSetting(_backTranslate2Step, SettingType.Consultant2StepLanguage,
                false, ConsultantLanguage2);

            //Segment step settings
            Workflow.SetStepSetting(_backTranslateStep, SettingType.DoSegmentBackTranslate, SegmentIsActive);
            Workflow.SetStepSetting(_backTranslateStep, SettingType.RequireSegmentBTSectionListen,
                SegmentRequireSectionListen);
            Workflow.SetStepSetting(_backTranslateStep, SettingType.RequireSegmentBTPassageListen,
                SegmentRequirePassageListen);
            Workflow.SetStepSetting(_backTranslateStep, SettingType.DoSegmentBTPassageReview,
                SegmentDoPassageReview);
            Workflow.SetStepSetting(_backTranslateStep, SettingType.RequireSegmentBTPassageReview,
                SegmentRequirePassageReview);
            Workflow.SetStepSetting(_backTranslateStep, SettingType.SegmentConsultantLanguage,
                false, SegmentConsultantLanguage);

            //Segment 2 step settings
            Workflow.SetStepSetting(_backTranslate2Step, SettingType.DoSegmentBackTranslate, Segment2IsActive);
            Workflow.SetStepSetting(_backTranslate2Step, SettingType.RequireSegmentBTSectionListen,
                Segment2RequireSectionListen);
            Workflow.SetStepSetting(_backTranslate2Step, SettingType.RequireSegmentBTPassageListen,
                Segment2RequirePassageListen);
            Workflow.SetStepSetting(_backTranslate2Step, SettingType.DoSegmentBTPassageReview,
                Segment2DoPassageReview);
            Workflow.SetStepSetting(_backTranslate2Step, SettingType.RequireSegmentBTPassageReview,
                Segment2RequirePassageReview);
            Workflow.SetStepSetting(_backTranslate2Step, SettingType.SegmentConsultantLanguage,
                false, SegmentConsultantLanguage);
            Workflow.SetStepSetting(_backTranslate2Step, SettingType.SegmentConsultant2StepLanguage,
                false, SegmentConsultantLanguage2);

            //Note Interpret step settings
            foreach (var step in _noteInterpretSteps)
            {
                Workflow.SetStepSetting(step, SettingType.IsActive, NoteInterpretIsActive);
                Workflow.SetStepSetting(step, SettingType.RequireNoteListen, RequireNoteListen);
                Workflow.SetStepSetting(step, SettingType.DoNoteReview, DoNoteReview);
                if (DoNoteReview)
                    Workflow.SetStepSetting(step, SettingType.RequireNoteReview, RequireNoteReview);
            }

            //Transcribe step settings
            Workflow.SetStepSetting(_transcribeStep, SettingType.DoPassageTranscribe, DoPassageTranscribeIsActive);
            Workflow.SetStepSetting(_transcribeStep, SettingType.RequirePassageTranscribeListen, RequirePassageTranscribeListenIsActive);
            Workflow.SetStepSetting(_transcribeStep, SettingType.DoSegmentTranscribe, SegmentTranscribeIsActive);
            Workflow.SetStepSetting(_transcribeStep, SettingType.RequireSegmentTranscribeListen, RequireSegmentTranscribeListenIsActive);

            Workflow.SetStepSetting(_transcribe2Step, SettingType.DoPassageTranscribe, DoPassage2TranscribeIsActive);
            Workflow.SetStepSetting(_transcribe2Step, SettingType.RequirePassageTranscribeListen, RequirePassage2TranscribeListenIsActive);
            Workflow.SetStepSetting(_transcribe2Step, SettingType.DoSegmentTranscribe, Segment2TranscribeIsActive);
            Workflow.SetStepSetting(_transcribe2Step, SettingType.RequireSegmentTranscribeListen, RequireSegment2TranscribeListenIsActive);

            //Revise step settings
            Workflow.SetStepSetting(_reviseStep, SettingType.AllowEditing, TranslateAllowEditing);

            Workflow.SetStepSetting(_reviseStep, SettingType.RequireNoteListen, TranslateRequireNoteListen);

            Workflow.SetStepSetting(_reviseStep, SettingType.DoPassageReview, TranslateDoPassageReview);
            if (TranslateDoPassageReview)
                Workflow.SetStepSetting(_reviseStep, SettingType.RequirePassageReview, TranslateRequirePassageReview);

            base.UpdateWorkflow();
        }
    }
}