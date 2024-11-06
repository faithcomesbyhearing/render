using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;
using Render.Resources.Localization;

namespace Render.Components.StageSettings.ConsultantCheckStageSettings
{
    public partial class ConsultantCheckStageSettings
    {
        public ConsultantCheckStageSettings()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.Bind(ViewModel, vm => vm.StageName, v => v.StageName.Text));
                d(this.Bind(ViewModel, vm => vm.CheckStepName.StepName, v => v.CheckStepName.Text));
                d(this.Bind(ViewModel, vm => vm.ReviseStepName.StepName, v => v.ReviseStepName.Text));
                d(this.Bind(ViewModel, vm => vm.BackTranslateStepName.StepName, v => v.BackTranslateStepName.Text));

                d(this.Bind(ViewModel, vm => vm.TranscribeStepName.StepName, v => v.TranscribeStepName.Text));
                d(this.Bind(ViewModel, vm => vm.TranscribeStepName.StepName, v => v.SegmentTranscribeStepName.Text));
                d(this.Bind(ViewModel, vm => vm.InterpretToConsultantStepName.StepName, v => v.InterpretToConsultantStepName.Text));
                d(this.Bind(ViewModel, vm => vm.InterpretToTranslatorStepName.StepName, v => v.InterpretToTranslatorStepName.Text));
                d(this.OneWayBind(ViewModel, vm => vm.DoStepBackTranslation, v => v.IncludeBackTranslateToggle.IsToggled));
                d(this.OneWayBind(ViewModel, vm => vm.DoStepBackTranslation, v => v.Do2StepToggle.IsEnabled));

                d(this.WhenAnyValue(x => x.IncludeBackTranslateToggle.IsToggled)
                    .Skip(1)
                    .Subscribe(async backTranslate =>
                    {
                        if (!backTranslate && ViewModel.AllowStepBackTranslation)
                        {
                            var result = await ViewModel.ConfirmStepDeactivationAsync();
                            if (result != DialogResult.Ok)
                            {
                                IncludeBackTranslateToggle.IsToggled = true;
                                return;
                            }
                        }

                        ViewModel.DoStepBackTranslation = backTranslate;
                        if (!backTranslate && ViewModel.Allow2StepBackTranslation)
                        {
                            Do2StepToggle.IsToggled = false;
                        }
                    }));
                d(this.OneWayBind(ViewModel, vm => vm.Allow2StepBackTranslation,
                    v => v.Do2StepToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.Do2StepBackTranslation,
                    v => v.Do2StepToggle.IsToggled));
                d(this.WhenAnyValue(x => x.Do2StepToggle.IsToggled)
                    .Skip(1)
                    .Subscribe(async twoStepBackTranslate =>
                    {
                        if (!twoStepBackTranslate && ViewModel.Allow2StepBackTranslation && Do2StepToggle.IsEnabled)
                        {
                            var result = await ViewModel.ConfirmStepDeactivationAsync();
                            if (result != DialogResult.Ok)
                            {
                                Do2StepToggle.IsToggled = true;
                                return;
                            }
                        }

                        ViewModel.Do2StepBackTranslation = twoStepBackTranslate;
                    }));
                d(this.Bind(ViewModel, vm => vm.CheckRequireNoteListen,
                    v => v.RequireConsultantCheckNoteListenToggle.IsToggled));
                d(this.OneWayBind(ViewModel, vm => vm.DoStepBackTranslation,
                    v => v.IncludePassageBackTranslateToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.RetellStepBackTranslation,
                   v => v.IncludePassageBackTranslateToggle.IsToggled));
                d(this.WhenAnyValue(x => x.IncludePassageBackTranslateToggle.IsToggled)
                    .Skip(1)
                    .Subscribe(async b =>
                    {
                        if (!b && ViewModel.DoStepBackTranslation)
                        {
                            var result = await ViewModel.ConfirmStepDeactivationAsync();
                            if (result != DialogResult.Ok)
                            {
                                IncludePassageBackTranslateToggle.IsToggled = true;
                                return;
                            }
                        }

                        ViewModel.RetellStepBackTranslation = b;
                    }));

                d(this.OneWayBind(ViewModel, vm => vm.DoStepBackTranslation,
                    v => v.IncludeSegmentBackTranslateToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.SegmentStepBackTranslation,
                   v => v.IncludeSegmentBackTranslateToggle.IsToggled));
                d(this.WhenAnyValue(x => x.IncludeSegmentBackTranslateToggle.IsToggled)
                    .Skip(1)
                    .Subscribe(async b =>
                    {
                        if (!b && ViewModel.DoStepBackTranslation)
                        {
                            var result = await ViewModel.ConfirmStepDeactivationAsync();
                            if (result != DialogResult.Ok)
                            {
                                IncludeSegmentBackTranslateToggle.IsToggled = true;
                                return;
                            }
                        }

                        ViewModel.SegmentStepBackTranslation = b;
                    }));
                d(this.OneWayBind(ViewModel, vm => vm.AllowTurnOnRetell2,
                    v => v.Step2RetellBackTranslateToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.AllowTurnOnRetell2,
                    v => v.Step2RetellBackTranslateLabel.IsEnabled));

                d(this.OneWayBind(ViewModel, vm => vm.AllowTurnOnSegment2,
                    v => v.Step2SegmentBackTranslateToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.AllowTurnOnSegment2,
                    v => v.Step2SegmentBackTranslateLabel.IsEnabled));

                //Retell bindings
                d(this.OneWayBind(ViewModel, vm => vm.RetellIsActive,
                    v => v.Step1RetellBackTranslateToggle.IsToggled));
                d(this.WhenAnyValue(x => x.Step1RetellBackTranslateToggle.IsToggled)
                    .Skip(1)
                    .Subscribe(async b =>
                    {
                        if (!b && ViewModel.DoStepBackTranslation && ViewModel.RetellIsActive)
                        {
                            var result = await ViewModel.ConfirmStepDeactivationAsync();
                            if (result != DialogResult.Ok)
                            {
                                Step1RetellBackTranslateToggle.IsToggled = true;
                                return;
                            }
                        }

                        ViewModel.RetellIsActive = b;
                    }));
                d(this.Bind(ViewModel, vm => vm.ConsultantLanguage,
                    v => v.RetellConsultantLanguage.Text));
                d(this.Bind(ViewModel, vm => vm.RetellRequireSectionListen,
                    v => v.RetellBtRequireSectionListenToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.RetellRequirePassageListen,
                    v => v.RetellBtRequirePassageListenToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.RetellDoPassageReview,
                    v => v.RetellBackTranslateDoPassageReviewToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.RetellRequirePassageReview,
                    v => v.RetellBtRequirePassageReviewToggle.IsToggled));
                d(this.OneWayBind(ViewModel, vm => vm.RetellDoPassageReview,
                    v => v.RetellBtRequirePassageReviewToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.RetellDoPassageReview,
                    v => v.RetellBtRequirePassageReviewLabel.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.RetellIsActive,
                    v => v.RetellBtRequirePassageListenToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.RetellIsActive,
                    v => v.RetellBtRequirePassageListenLabel.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.RetellIsActive,
                    v => v.RetellBackTranslateDoPassageReviewToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.RetellIsActive,
                    v => v.RetellBackTranslateDoPassageReviewLabel.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.RetellIsActive,
                    v => v.RetellBtRequireSectionListenToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.RetellIsActive,
                    v => v.RetellBtRequireSectionListenLabel.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.RetellIsActive,
                    v => v.RetellConsultantLanguage.IsReadOnly, x => !x));
                d(this.OneWayBind(ViewModel, vm => vm.RetellIsActive,
                    v => v.Step1LanguageFrame.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.RetellIsActive,
                    v => v.Step1Language.IsEnabled));

                //Retell2 bindings
                d(this.OneWayBind(ViewModel, vm => vm.Retell2IsActive,
                    v => v.Step2RetellBackTranslateToggle.IsToggled));
                d(this.WhenAnyValue(x => x.Step2RetellBackTranslateToggle.IsToggled)
                    .Skip(1)
                    .Subscribe(async b =>
                    {
                        if (!b && ViewModel.Do2StepBackTranslation && ViewModel.Retell2IsActive)
                        {
                            var result = await ViewModel.ConfirmStepDeactivationAsync();
                            if (result != DialogResult.Ok)
                            {
                                Step2RetellBackTranslateToggle.IsToggled = true;
                                return;
                            }
                        }

                        ViewModel.Retell2IsActive = b;
                    }));
                d(this.Bind(ViewModel, vm => vm.ConsultantLanguage2,
                    v => v.RetellConsultantLanguage2.Text));
                d(this.Bind(ViewModel, vm => vm.Retell2RequireSectionListen,
                    v => v.RetellBt2RequireSectionListenToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.Retell2RequirePassageListen,
                    v => v.RetellBt2RequirePassageListenToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.Retell2DoPassageReview,
                    v => v.RetellBackTranslate2DoPassageReviewToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.Retell2RequirePassageReview,
                    v => v.RetellBt2RequirePassageReviewToggle.IsToggled));
                d(this.OneWayBind(ViewModel, vm => vm.Retell2DoPassageReview,
                    v => v.RetellBt2RequirePassageReviewToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.Retell2DoPassageReview,
                    v => v.RetellBt2RequirePassageReviewLabel.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.Retell2IsActive,
                    v => v.RetellBt2RequirePassageListenToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.Retell2IsActive,
                    v => v.RetellBt2RequirePassageListenLabel.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.Retell2IsActive,
                    v => v.RetellBackTranslate2DoPassageReviewToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.Retell2IsActive,
                    v => v.RetellBackTranslate2DoPassageReviewLabel.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.Retell2IsActive,
                    v => v.RetellBt2RequireSectionListenToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.Retell2IsActive,
                    v => v.RetellBt2RequireSectionListenLabel.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.Retell2IsActive,
                   v => v.RetellConsultantLanguage2.IsReadOnly, x => !x));
                d(this.OneWayBind(ViewModel, vm => vm.Retell2IsActive,
                    v => v.Step2LanguageFrame.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.Retell2IsActive,
                    v => v.Step2Language.IsEnabled));

                SetPassageTranscribeBindings(d);

                //Segment Bindings
                d(this.OneWayBind(ViewModel, vm => vm.SegmentIsActive,
                    v => v.Step1SegmentBackTranslateToggle.IsToggled));
                d(this.WhenAnyValue(x => x.Step1SegmentBackTranslateToggle.IsToggled)
                    .Skip(1)
                    .Subscribe(async b =>
                    {
                        if (!b && ViewModel.DoStepBackTranslation && ViewModel.SegmentIsActive)
                        {
                            var result = await ViewModel.ConfirmStepDeactivationAsync();
                            if (result != DialogResult.Ok)
                            {
                                Step1SegmentBackTranslateToggle.IsToggled = true;
                                return;
                            }
                        }

                        ViewModel.SegmentIsActive = b;
                    }));
                d(this.Bind(ViewModel, vm => vm.SegmentConsultantLanguage,
                    v => v.SegmentConsultantLanguage.Text));
                d(this.Bind(ViewModel, vm => vm.SegmentRequireSectionListen,
                    v => v.SegmentBtRequireSectionListenToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.SegmentRequirePassageListen,
                    v => v.SegmentBtRequirePassageListenToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.SegmentDoPassageReview,
                    v => v.SegmentBackTranslateDoPassageReviewToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.SegmentRequirePassageReview,
                    v => v.SegmentBtRequirePassageReviewToggle.IsToggled));
                d(this.OneWayBind(ViewModel, vm => vm.SegmentDoPassageReview,
                    v => v.SegmentBtRequirePassageReviewToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.SegmentDoPassageReview,
                    v => v.SegmentBtRequirePassageReviewLabel.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.SegmentIsActive,
                    v => v.SegmentBtRequirePassageListenToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.SegmentIsActive,
                    v => v.SegmentBtRequirePassageListenLabel.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.SegmentIsActive,
                    v => v.SegmentBackTranslateDoPassageReviewToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.SegmentIsActive,
                    v => v.SegmentBackTranslateDoPassageReviewLabel.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.SegmentIsActive,
                    v => v.SegmentBtRequireSectionListenToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.SegmentIsActive,
                    v => v.SegmentBtRequireSectionListenLabel.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.SegmentIsActive,
                    v => v.SegmentConsultantLanguage.IsReadOnly, x => !x));
                d(this.OneWayBind(ViewModel, vm => vm.SegmentIsActive,
                    v => v.SegmentStep1LanguageFrame.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.SegmentIsActive,
                    v => v.SegmentStep1Language.IsEnabled));

                //Segment 2 Bindings
                d(this.OneWayBind(ViewModel, vm => vm.Segment2IsActive,
                    v => v.Step2SegmentBackTranslateToggle.IsToggled));
                d(this.WhenAnyValue(x => x.Step2SegmentBackTranslateToggle.IsToggled)
                    .Skip(1)
                    .Subscribe(async b =>
                    {
                        if (!b && ViewModel.Do2StepBackTranslation && ViewModel.Segment2IsActive)
                        {
                            var result = await ViewModel.ConfirmStepDeactivationAsync();
                            if (result != DialogResult.Ok)
                            {
                                Step2SegmentBackTranslateToggle.IsToggled = true;
                                return;
                            }
                        }

                        ViewModel.Segment2IsActive = b;
                    }));
                d(this.Bind(ViewModel, vm => vm.SegmentConsultantLanguage2,
                    v => v.SegmentConsultantLanguage2.Text));
                d(this.Bind(ViewModel, vm => vm.Segment2RequireSectionListen,
                    v => v.SegmentBt2RequireSectionListenToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.Segment2RequirePassageListen,
                    v => v.SegmentBt2RequirePassageListenToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.Segment2DoPassageReview,
                    v => v.SegmentBackTranslate2DoPassageReviewToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.Segment2RequirePassageReview,
                    v => v.SegmentBt2RequirePassageReviewToggle.IsToggled));
                d(this.OneWayBind(ViewModel, vm => vm.Segment2DoPassageReview,
                    v => v.SegmentBt2RequirePassageReviewToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.Segment2DoPassageReview,
                    v => v.SegmentBt2RequirePassageReviewLabel.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.Segment2IsActive,
                    v => v.SegmentBt2RequirePassageListenToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.Segment2IsActive,
                    v => v.SegmentBt2RequirePassageListenLabel.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.Segment2IsActive,
                    v => v.SegmentBackTranslate2DoPassageReviewToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.Segment2IsActive,
                    v => v.SegmentBackTranslate2DoPassageReviewLabel.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.Segment2IsActive,
                    v => v.SegmentBt2RequireSectionListenToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.Segment2IsActive,
                    v => v.SegmentBt2RequireSectionListenLabel.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.Segment2IsActive,
                    v => v.SegmentConsultantLanguage2.IsReadOnly, x => !x));
                d(this.OneWayBind(ViewModel, vm => vm.Segment2IsActive,
                    v => v.SegmentStep2LanguageFrame.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.Segment2IsActive,
                    v => v.SegmentStep2Language.IsEnabled));

                SetSegmentTranscribeBindings(d);

                //Note Interpret Bindings
                d(this.OneWayBind(ViewModel, vm => vm.NoteInterpretIsActive,
                    v => v.DoNoteInterpretToggle.IsToggled));
                d(this.WhenAnyValue(x => x.DoNoteInterpretToggle.IsToggled)
                    .Skip(1)
                    .Subscribe(async b =>
                    {
                        if (!b)
                        {
                            var result = await ViewModel.ConfirmStepDeactivationAsync();
                            if (result != DialogResult.Ok)
                            {
                                DoNoteInterpretToggle.IsToggled = true;
                                return;
                            }
                        }

                        ViewModel.NoteInterpretIsActive = b;
                    }));
                d(this.Bind(ViewModel, vm => vm.RequireNoteListen,
                    v => v.RequireNoteListenToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.DoNoteReview,
                    v => v.DoNoteReviewToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.RequireNoteReview,
                    v => v.RequireNoteReviewToggle.IsToggled));
                d(this.OneWayBind(ViewModel, vm => vm.NoteInterpretIsActive,
                    v => v.RequireNoteListenToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.NoteInterpretIsActive,
                    v => v.RequireNoteListenLabel.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.NoteInterpretIsActive,
                    v => v.DoNoteReviewToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.NoteInterpretIsActive,
                    v => v.DoNoteReviewLabel.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.NoteInterpretIsActive,
                    v => v.RequireNoteReviewToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.NoteInterpretIsActive,
                    v => v.RequireNoteReviewLabel.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.NoteInterpretIsActive,
                    v => v.InterpretToConsultantStepNameBorder.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.NoteInterpretIsActive,
                    v => v.InterpretToTranslatorStepNameBorder.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.DoNoteReview,
                    v => v.RequireNoteReviewToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.DoNoteReview,
                    v => v.RequireNoteReviewLabel.IsEnabled));

                //Revise bindings
                d(this.Bind(ViewModel, vm => vm.TranslateRequireNoteListen,
                    v => v.ReviseRequireNoteListenToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.TranslateDoPassageReview,
                    v => v.DoPassageReviewToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.TranslateRequirePassageReview,
                    v => v.RequirePassageReviewToggle.IsToggled));
                d(this.Bind(ViewModel, vm => vm.TranslateAllowEditing,
                    v => v.AllowEditingToggle.IsToggled));

                d(this.OneWayBind(ViewModel, vm => vm.RetellStepBackTranslation,
                    v => v.Step1RetellBackTranslateToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.RetellStepBackTranslation,
                    v => v.Step1RetellBackTranslateLabel.IsEnabled));

                d(this.OneWayBind(ViewModel, vm => vm.RetellStepBackTranslation,
                    v => v.Step2RetellBackTranslateToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.RetellStepBackTranslation,
                    v => v.Step2RetellBackTranslateLabel.IsEnabled));

                d(this.OneWayBind(ViewModel, vm => vm.SegmentStepBackTranslation,
                    v => v.Step1SegmentBackTranslateToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.SegmentStepBackTranslation,
                    v => v.Step1SegmentBackTranslateLabel.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.SegmentStepBackTranslation,
                    v => v.Step2SegmentBackTranslateToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.SegmentStepBackTranslation,
                    v => v.Step2SegmentBackTranslateLabel.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.TranslateDoPassageReview,
                    v => v.RequirePassageReviewToggle.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.TranslateDoPassageReview,
                    v => v.RequirePassageReviewLabel.IsEnabled));

                d(this.WhenAnyValue(x => x.ViewModel.Do2StepBackTranslation)
                    .Subscribe(x =>
                    {
                        Step1RetellFrame.SetValue(IsVisibleProperty, x);
                        Step1Step2Separator.SetValue(IsVisibleProperty, x);
                        Step1Step2SeparatorRow2.SetValue(IsVisibleProperty, x);
                        Step2RetellBackTranslateStack.SetValue(IsVisibleProperty, x);
                        Step2RetellBackTranslateStackRow2.SetValue(IsVisibleProperty, x);
                        Step1RetellBackTranslateStack.SetValue(Grid.ColumnSpanProperty, x ? 1 : 3);
                        Step1RetellBackTranslateStackRow2.SetValue(Grid.ColumnSpanProperty, x ? 1 : 3);
                        Step1SegmentBackTranslateLabel.SetValue(IsVisibleProperty, x);
                        Step1SegmentFrame.SetValue(IsVisibleProperty, x);
                        Step2SegmentBackTranslateStack.SetValue(IsVisibleProperty, x);
                        Step2SegmentBackTranslateStackRow2.SetValue(IsVisibleProperty, x);
                        Step1Step2SegmentsSeparator.SetValue(IsVisibleProperty, x);
                        Step1Step2SegmentsSeparatorRow2.SetValue(IsVisibleProperty, x);
                        Step1SegmentBackTranslateStack.SetValue(Grid.ColumnSpanProperty, x ? 1 : 3);
                        Step1SegmentBackTranslateStackRow2.SetValue(Grid.ColumnSpanProperty, x ? 1 : 3);
                        Step1Language.Text = x ? AppResources.IntermediateLanguage : AppResources.ConsultantLanguage;
                        RetellConsultantLanguage.Placeholder = x ? AppResources.Language : AppResources.EnterLanguage;
                        SegmentConsultantLanguage.Placeholder = x ? AppResources.Language : AppResources.EnterLanguage;
                        SegmentStep1Language.Text = x ? AppResources.IntermediateLanguage : AppResources.ConsultantLanguage;
                    }));

                d(this.WhenAnyValue(x => x.ViewModel.Do2StepBackTranslation, x => x.ViewModel.TranscribeStepName.StepName)
                    .Subscribe(((bool Do2StepBackTranslation, string TranscribeStepName) options) =>
                    {
                        if (options.Do2StepBackTranslation)
                        {
                            DoPassageTranscribeLabel.Text = AppResources.Step1;
                            DoSegmentTranscribeLabel.Text = AppResources.Step1;
                            return;
                        }

                        DoPassageTranscribeLabel.Text = ViewModel?.TranscribeStepName.StepName;
                        DoSegmentTranscribeLabel.Text = ViewModel?.TranscribeStepName.StepName;

                    }));

                d(this.WhenAnyValue(x => x.ViewModel.FlowDirection)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(flowDirection =>
                    {
                        TopLevelElement.SetValue(FlowDirectionProperty, flowDirection);
                    }));
            });
        }

        private void SetSegmentTranscribeBindings(Action<IDisposable> d)
        {
            d(this.Bind(ViewModel, vm => vm.SegmentTranscribeIsActive,
                v => v.DoSegmentTranscribeToggle.IsToggled));
            d(this.WhenAnyValue(x => x.DoSegmentTranscribeToggle.IsToggled)
                .Skip(1)
                .Subscribe(async b =>
                {
                    if (!b && ViewModel.SegmentIsActive)
                    {
                        var result = await ViewModel.ConfirmStepDeactivationAsync();
                        if (result != DialogResult.Ok)
                        {
                            DoSegmentTranscribeToggle.IsToggled = true;
                            return;
                        }
                    }

                    ViewModel.SegmentTranscribeIsActive = b;
                }));
            d(this.OneWayBind(ViewModel, vm => vm.SegmentIsActive,
                v => v.DoSegmentTranscribeToggle.IsEnabled));
            d(this.OneWayBind(ViewModel, vm => vm.SegmentIsActive,
                v => v.DoSegmentTranscribeLabel.IsEnabled));

            d(this.Bind(ViewModel, vm => vm.RequireSegmentTranscribeListenIsActive,
                v => v.RequireSegmentTranscribeListenToggle.IsToggled));
            d(this.OneWayBind(ViewModel, vm => vm.SegmentTranscribeIsActive,
                v => v.RequireSegmentTranscribeListenToggle.IsEnabled));
            d(this.OneWayBind(ViewModel, vm => vm.SegmentTranscribeIsActive,
                v => v.RequireSegmentTranscribeListenLabel.IsEnabled));

            d(this.Bind(ViewModel, vm => vm.Segment2TranscribeIsActive,
                v => v.DoSegment2TranscribeToggle.IsToggled));
            d(this.WhenAnyValue(x => x.DoSegment2TranscribeToggle.IsToggled)
                .Skip(1)
                .Subscribe(async b =>
                {
                    if (!b && ViewModel.SegmentIsActive && ViewModel.Segment2IsActive)
                    {
                        var result = await ViewModel.ConfirmStepDeactivationAsync();
                        if (result != DialogResult.Ok)
                        {
                            DoSegment2TranscribeToggle.IsToggled = true;
                            return;
                        }
                    }

                    ViewModel.Segment2TranscribeIsActive = b;
                }));
            d(this.OneWayBind(ViewModel, vm => vm.Segment2IsActive,
                v => v.DoSegment2TranscribeToggle.IsEnabled));
            d(this.OneWayBind(ViewModel, vm => vm.Segment2IsActive,
                v => v.DoSegment2TranscribeLabel.IsEnabled));
            d(this.Bind(ViewModel, vm => vm.RequireSegment2TranscribeListenIsActive,
                v => v.RequireSegment2TranscribeListenToggle.IsToggled));
            d(this.OneWayBind(ViewModel, vm => vm.Segment2TranscribeIsActive,
                v => v.RequireSegment2TranscribeListenToggle.IsEnabled));
            d(this.OneWayBind(ViewModel, vm => vm.Segment2TranscribeIsActive,
                v => v.RequireSegment2TranscribeListenLabel.IsEnabled));
        }

        private void SetPassageTranscribeBindings(Action<IDisposable> d)
        {
            d(this.Bind(ViewModel, vm => vm.DoPassageTranscribeIsActive,
                v => v.DoPassageTranscribeToggle.IsToggled));
            d(this.WhenAnyValue(x => x.DoPassageTranscribeToggle.IsToggled)
                .Skip(1)
                .Subscribe(async b =>
                {
                    if (!b && ViewModel.RetellIsActive)
                    {
                        var result = await ViewModel.ConfirmStepDeactivationAsync();
                        if (result != DialogResult.Ok)
                        {
                            DoPassageTranscribeToggle.IsToggled = true;
                            return;
                        }
                    }

                    ViewModel.DoPassageTranscribeIsActive = b;
                }));

            d(this.OneWayBind(ViewModel, vm => vm.RetellIsActive,
                v => v.DoPassageTranscribeToggle.IsEnabled));
            d(this.OneWayBind(ViewModel, vm => vm.RetellIsActive,
                v => v.DoPassageTranscribeLabel.IsEnabled));

            d(this.Bind(ViewModel, vm => vm.RequirePassageTranscribeListenIsActive,
                v => v.RequirePassageTranscribeListenToggle.IsToggled));
            d(this.OneWayBind(ViewModel, vm => vm.DoPassageTranscribeIsActive,
                v => v.RequirePassageTranscribeListenToggle.IsEnabled));
            d(this.OneWayBind(ViewModel, vm => vm.DoPassageTranscribeIsActive,
                v => v.RequirePassageTranscribeListenLabel.IsEnabled));
            d(this.Bind(ViewModel, vm => vm.DoPassage2TranscribeIsActive,
                v => v.DoPassage2TranscribeToggle.IsToggled));
            d(this.WhenAnyValue(x => x.DoPassage2TranscribeToggle.IsToggled)
                .Skip(1)
                .Subscribe(async b =>
                {
                    if (!b && ViewModel.RetellIsActive)
                    {
                        var result = await ViewModel.ConfirmStepDeactivationAsync();
                        if (result != DialogResult.Ok)
                        {
                            DoPassage2TranscribeToggle.IsToggled = true;
                            return;
                        }
                    }

                    ViewModel.DoPassage2TranscribeIsActive = b;
                }));
            d(this.OneWayBind(ViewModel, vm => vm.Retell2IsActive,
                v => v.DoPassage2TranscribeToggle.IsEnabled));
            d(this.OneWayBind(ViewModel, vm => vm.Retell2IsActive,
                v => v.DoPassage2TranscribeLabel.IsEnabled));
            d(this.Bind(ViewModel, vm => vm.RequirePassage2TranscribeListenIsActive,
                v => v.RequirePassage2TranscribeListenToggle.IsToggled));
            d(this.OneWayBind(ViewModel, vm => vm.DoPassage2TranscribeIsActive,
                v => v.RequirePassage2TranscribeListenToggle.IsEnabled));
            d(this.OneWayBind(ViewModel, vm => vm.DoPassage2TranscribeIsActive,
                v => v.RequirePassage2TranscribeListenLabel.IsEnabled));

            d(this.WhenAnyValue(
                x => x.ViewModel.RetellIsActive,
                x => x.ViewModel.DoPassageTranscribeIsActive,
                x => x.ViewModel.DoPassage2TranscribeIsActive)
                   .Subscribe(((bool RetellIsActive, bool DoPassageTranscribeIsActive, bool DoPassage2TranscribeIsActive) options) =>
                   {
                       TranscribeStepNameBorder.IsEnabled = options.RetellIsActive
                            && (options.DoPassageTranscribeIsActive || options.DoPassage2TranscribeIsActive);

                   }));

            d(this.WhenAnyValue(
                x => x.ViewModel.DoStepBackTranslation,
                x => x.ViewModel.SegmentTranscribeIsActive,
                x => x.ViewModel.Segment2TranscribeIsActive)
                    .Subscribe(((bool DoStepBackTranslation, bool SegmentTranscribeIsActive, bool Segment2TranscribeIsActive) options) =>
                    {
                        SegmentTranscribeStepNameBorder.IsEnabled = options.DoStepBackTranslation
                            && (options.SegmentTranscribeIsActive || options.Segment2TranscribeIsActive);

                    }));

        }

        #region Toggle event methods
        private void IncludeBackTranslateToggleTapped(object sender, EventArgs e)
        {
            IncludeBackTranslateToggle.IsToggled = !IncludeBackTranslateToggle.IsToggled;
        }

        private void Do2StepToggleTapped(object sender, EventArgs e)
        {
            if (Do2StepToggle.IsEnabled)
            {
                Do2StepToggle.IsToggled = !Do2StepToggle.IsToggled;
            }
        }

        private void DoRetellBackTranslateToggleTapped(object sender, EventArgs e)
        {
            if (Step1RetellBackTranslateToggle.IsEnabled)
            {
                Step1RetellBackTranslateToggle.IsToggled = !Step1RetellBackTranslateToggle.IsToggled;
            }
        }

        private void RetellBtRequireSectionListenToggleTapped(object sender, EventArgs e)
        {
            if (RetellBtRequireSectionListenToggle.IsEnabled)
            {
                RetellBtRequireSectionListenToggle.IsToggled = !RetellBtRequireSectionListenToggle.IsToggled;
            }
        }

        private void RetellBtRequirePassageListenToggleTapped(object sender, EventArgs e)
        {
            if (RetellBtRequirePassageListenToggle.IsEnabled)
            {
                RetellBtRequirePassageListenToggle.IsToggled = !RetellBtRequirePassageListenToggle.IsToggled;
            }
        }

        private void RetellBackTranslateDoPassageReviewToggleTapped(object sender, EventArgs e)
        {
            if (RetellBackTranslateDoPassageReviewToggle.IsEnabled)
            {
                RetellBackTranslateDoPassageReviewToggle.IsToggled = !RetellBackTranslateDoPassageReviewToggle.IsToggled;
            }
        }

        private void RetellBtRequirePassageReviewToggleTapped(object sender, EventArgs e)
        {
            if (RetellBtRequirePassageReviewToggle.IsEnabled)
            {
                RetellBtRequirePassageReviewToggle.IsToggled = !RetellBtRequirePassageReviewToggle.IsToggled;
            }
        }

        private void DoPassageTranscribeToggleTapped(object sender, EventArgs e)
        {
            if (DoPassageTranscribeToggle.IsEnabled)
            {
                DoPassageTranscribeToggle.IsToggled = !DoPassageTranscribeToggle.IsToggled;
            }
        }

        private void DoRetellBackTranslate2ToggleTapped(object sender, EventArgs e)
        {
            if (Step2RetellBackTranslateToggle.IsEnabled)
            {
                Step2RetellBackTranslateToggle.IsToggled = !Step2RetellBackTranslateToggle.IsToggled;
            }
        }

        private void RetellBt2RequireSectionListenToggleTapped(object sender, EventArgs e)
        {
            if (RetellBt2RequireSectionListenToggle.IsEnabled)
            {
                RetellBt2RequireSectionListenToggle.IsToggled = !RetellBt2RequireSectionListenToggle.IsToggled;
            }
        }

        private void RetellBt2RequirePassageListenToggleTapped(object sender, EventArgs e)
        {
            if (RetellBt2RequirePassageListenToggle.IsEnabled)
            {
                RetellBt2RequirePassageListenToggle.IsToggled = !RetellBt2RequirePassageListenToggle.IsToggled;
            }
        }

        private void RetellBackTranslate2DoPassageReviewToggleTapped(object sender, EventArgs e)
        {
            if (RetellBackTranslate2DoPassageReviewToggle.IsEnabled)
            {
                RetellBackTranslate2DoPassageReviewToggle.IsToggled = !RetellBackTranslate2DoPassageReviewToggle.IsToggled;
            }
        }

        private void RetellBt2RequirePassageReviewToggleTapped(object sender, EventArgs e)
        {
            if (RetellBt2RequirePassageReviewToggle.IsEnabled)
            {
                RetellBt2RequirePassageReviewToggle.IsToggled = !RetellBt2RequirePassageReviewToggle.IsToggled;
            }
        }

        private void DoPassage2TranscribeToggleTapped(object sender, EventArgs e)
        {
            if (DoPassage2TranscribeToggle.IsEnabled)
            {
                DoPassage2TranscribeToggle.IsToggled = !DoPassage2TranscribeToggle.IsToggled;
            }
        }

        private void DoSegmentBackTranslateToggleTapped(object sender, EventArgs e)
        {
            if (Step1SegmentBackTranslateToggle.IsEnabled)
            {
                Step1SegmentBackTranslateToggle.IsToggled = !Step1SegmentBackTranslateToggle.IsToggled;
            }
        }

        private void SegmentBtRequireSectionListenToggleTapped(object sender, EventArgs e)
        {
            if (SegmentBtRequireSectionListenToggle.IsEnabled)
            {
                SegmentBtRequireSectionListenToggle.IsToggled = !SegmentBtRequireSectionListenToggle.IsToggled;
            }
        }

        private void SegmentBtRequirePassageListenToggleTapped(object sender, EventArgs e)
        {
            if (SegmentBtRequirePassageListenToggle.IsEnabled)
            {
                SegmentBtRequirePassageListenToggle.IsToggled = !SegmentBtRequirePassageListenToggle.IsToggled;
            }
        }

        private void SegmentBackTranslateDoPassageReviewToggleTapped(object sender, EventArgs e)
        {
            if (SegmentBackTranslateDoPassageReviewToggle.IsEnabled)
            {
                SegmentBackTranslateDoPassageReviewToggle.IsToggled = !SegmentBackTranslateDoPassageReviewToggle.IsToggled;
            }
        }

        private void SegmentBtRequirePassageReviewToggleTapped(object sender, EventArgs e)
        {
            if (SegmentBtRequirePassageReviewToggle.IsEnabled)
            {
                SegmentBtRequirePassageReviewToggle.IsToggled = !SegmentBtRequirePassageReviewToggle.IsToggled;
            }
        }

        private void DoSegmentTranscribeToggleTapped(object sender, EventArgs e)
        {
            if (DoSegmentTranscribeToggle.IsEnabled)
            {
                DoSegmentTranscribeToggle.IsToggled = !DoSegmentTranscribeToggle.IsToggled;
            }
        }

        private void DoSegmentBackTranslate2ToggleTapped(object sender, EventArgs e)
        {
            if (Step2SegmentBackTranslateToggle.IsEnabled)
            {
                Step2SegmentBackTranslateToggle.IsToggled = !Step2SegmentBackTranslateToggle.IsToggled;
            }
        }

        private void SegmentBt2RequireSectionListenToggleTapped(object sender, EventArgs e)
        {
            if (SegmentBt2RequireSectionListenToggle.IsEnabled)
            {
                SegmentBt2RequireSectionListenToggle.IsToggled = !SegmentBt2RequireSectionListenToggle.IsToggled;
            }
        }

        private void SegmentBt2RequirePassageListenToggleTapped(object sender, EventArgs e)
        {
            if (SegmentBt2RequirePassageListenToggle.IsEnabled)
            {
                SegmentBt2RequirePassageListenToggle.IsToggled = !SegmentBt2RequirePassageListenToggle.IsToggled;
            }
        }

        private void SegmentBackTranslate2DoPassageReviewToggleTapped(object sender, EventArgs e)
        {
            if (SegmentBackTranslate2DoPassageReviewToggle.IsEnabled)
            {
                SegmentBackTranslate2DoPassageReviewToggle.IsToggled = !SegmentBackTranslate2DoPassageReviewToggle.IsToggled;
            }
        }

        private void SegmentBtRequire2PassageReviewToggleTapped(object sender, EventArgs e)
        {
            if (SegmentBt2RequirePassageReviewToggle.IsEnabled)
            {
                SegmentBt2RequirePassageReviewToggle.IsToggled = !SegmentBt2RequirePassageReviewToggle.IsToggled;
            }
        }

        private void DoSegment2TranscribeToggleTapped(object sender, EventArgs e)
        {
            if (DoSegment2TranscribeToggle.IsEnabled)
            {
                DoSegment2TranscribeToggle.IsToggled = !DoSegment2TranscribeToggle.IsToggled;
            }
        }

        private void DoNoteInterpretToggleTapped(object sender, EventArgs e)
        {
            DoNoteInterpretToggle.IsToggled = !DoNoteInterpretToggle.IsToggled;
        }

        private void RequireNoteListenToggleTapped(object sender, EventArgs e)
        {
            if (RequireNoteListenToggle.IsEnabled)
            {
                RequireNoteListenToggle.IsToggled = !RequireNoteListenToggle.IsToggled;
            }
        }

        private void DoNoteReviewToggleTapped(object sender, EventArgs e)
        {
            if (DoNoteReviewToggle.IsEnabled)
            {
                DoNoteReviewToggle.IsToggled = !DoNoteReviewToggle.IsToggled;
            }
        }

        private void RequireNoteReviewToggleTapped(object sender, EventArgs e)
        {
            if (RequireNoteReviewToggle.IsEnabled)
            {
                RequireNoteReviewToggle.IsToggled = !RequireNoteReviewToggle.IsToggled;
            }
        }

        private void ReviseRequireNoteListenToggleTapped(object sender, EventArgs e)
        {
            if (ReviseRequireNoteListenToggle.IsEnabled)
            {
                ReviseRequireNoteListenToggle.IsToggled = !ReviseRequireNoteListenToggle.IsToggled;
            }
        }

        private void DoPassageReviewToggleTapped(object sender, EventArgs e)
        {
            DoPassageReviewToggle.IsToggled = !DoPassageReviewToggle.IsToggled;
        }

        private void RequirePassageReviewToggleTapped(object sender, EventArgs e)
        {
            if (RequirePassageReviewToggle.IsEnabled)
            {
                RequirePassageReviewToggle.IsToggled = !RequirePassageReviewToggle.IsToggled;
            }
        }

        private void AllowEditingToggleTapped(object sender, EventArgs e)
        {
            AllowEditingToggle.IsToggled = !AllowEditingToggle.IsToggled;
        }

        private void RequireConsultantCheckNoteListenToggleTapped(object sender, EventArgs e)
        {
            if (RequireConsultantCheckNoteListenToggle.IsEnabled)
            {
                RequireConsultantCheckNoteListenToggle.IsToggled = !RequireConsultantCheckNoteListenToggle.IsToggled;
            }
        }

        #endregion

        #region Stack expand methods

        private void ReviseNoteListenStackTapped(object sender, EventArgs e)
        {
            ReviseNoteListenStack.IsVisible = !ReviseNoteListenStack.IsVisible;

            ReviseNoteListenExpandStackIcon.IsVisible = !ReviseNoteListenStack.IsVisible;
            ReviseNoteListenCollapseStackIcon.IsVisible = ReviseNoteListenStack.IsVisible;

            if (ReviseNoteListenStack.IsVisible)
            {
                ScrollToElement(ReviseNoteListenStackWrapper);
            }
        }

        private void PassageReviewStackTapped(object sender, EventArgs e)
        {
            PassageReviewStack.IsVisible = !PassageReviewStack.IsVisible;

            PassageReviewExpandStackIcon.IsVisible = !PassageReviewStack.IsVisible;
            PassageReviewCollapseStackIcon.IsVisible = PassageReviewStack.IsVisible;

            if (PassageReviewStack.IsVisible)
            {
                ScrollToElement(PassageReviewStackWrapper);
            }
        }

        private void NoteInterpretStackTapped(object sender, EventArgs e)
        {
            NoteInterpretStack.IsVisible = !NoteInterpretStack.IsVisible;

            NoteInterpretExpandStackIcon.IsVisible = !NoteInterpretStack.IsVisible;
            NoteInterpretCollapseStackIcon.IsVisible = NoteInterpretStack.IsVisible;

            if (NoteInterpretStack.IsVisible)
            {
                ScrollToElement(NoteInterpretStackWrapper);
            }
        }

        private void RetellStackTapped(object sender, EventArgs e)
        {
            RetellStack.IsVisible = !RetellStack.IsVisible;

            RetellExpandStackIcon.IsVisible = !RetellStack.IsVisible;
            RetellCollapseStackIcon.IsVisible = RetellStack.IsVisible;

            if (RetellStack.IsVisible)
            {
                ScrollToElement(RetellStackWrapper);
            }
        }

        private void SegmentStackTapped(object sender, EventArgs e)
        {
            SegmentStack.IsVisible = !SegmentStack.IsVisible;

            SegmentExpandStackIcon.IsVisible = !SegmentStack.IsVisible;
            SegmentCollapseStackIcon.IsVisible = SegmentStack.IsVisible;

            if (SegmentStack.IsVisible)
            {
                ScrollToElement(SegmentStackWrapper);
            }
        }

        private void ConsultantCheckNoteListenStackTapped(object sender, EventArgs e)
        {
            ConsultantCheckNoteListenStack.IsVisible = !ConsultantCheckNoteListenStack.IsVisible;

            ConsultantCheckNoteListenExpandStackIcon.IsVisible = !ConsultantCheckNoteListenStack.IsVisible;
            ConsultantCheckNoteListenCollapseStackIcon.IsVisible = ConsultantCheckNoteListenStack.IsVisible;

            if (ConsultantCheckNoteListenStack.IsVisible)
            {
                ScrollToElement(ConsultantCheckNoteListenStackWrapper);
            }
        }

        /// <summary>
        /// Short delay is needed to make sure that 
        /// we are going to scroll after all necessary layout calculations only.
        /// </summary>
        private async void ScrollToElement(Element element)
        {
            await Task.Delay(50);
            await scrollView.ScrollToAsync(element, ScrollToPosition.Start, true);
        }

        #endregion

        private void IncludePassageBackTranslateToggleTapped(object sender, EventArgs e)
        {
            if (IncludePassageBackTranslateToggle.IsEnabled)
            {
                IncludePassageBackTranslateToggle.IsToggled = !IncludePassageBackTranslateToggle.IsToggled;
            }
        }

        private void IncludeSegmentBackTranslateToggleTapped(object sender, EventArgs e)
        {
            if (IncludeSegmentBackTranslateToggle.IsEnabled)
            {
                IncludeSegmentBackTranslateToggle.IsToggled = !IncludeSegmentBackTranslateToggle.IsToggled;
            }
        }

        private void RequirePassageTranscribeListenToggleTapped(object sender, EventArgs e)
        {
            var toggle = RequirePassageTranscribeListenToggle;

            if (toggle.IsEnabled)
            {
                toggle.IsToggled = !toggle.IsToggled;
            }
        }

        private void RequirePassage2TranscribeListenToggleTapped(object sender, EventArgs e)
        {
            var toggle = RequirePassage2TranscribeListenToggle;

            if (toggle.IsEnabled)
            {
                toggle.IsToggled = !toggle.IsToggled;
            }
        }

        private void RequireSegmentTranscribeListenToggleTapped(object sender, EventArgs e)
        {
            var toggle = RequireSegmentTranscribeListenToggle;

            if (toggle.IsEnabled)
            {
                toggle.IsToggled = !toggle.IsToggled;
            }
        }

        private void RequireSegment2TranscribeListenToggleTapped(object sender, EventArgs e)
        {
            var toggle = RequireSegment2TranscribeListenToggle;

            if (toggle.IsEnabled)
            {
                toggle.IsToggled = !toggle.IsToggled;
            }
        }
    }
}