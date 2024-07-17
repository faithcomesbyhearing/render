using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;
using Render.Utilities;

namespace Render.Pages.Configurator.WorkflowAssignment.Stages;

 public partial class WorkflowConsultantCheckStageColumn 
    {
        public WorkflowConsultantCheckStageColumn()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
				d(this.OneWayBind(ViewModel, vm => vm.Name, v => v.StageName.Text,
					TailTruncationHelper.AddTailTruncation));
                d(this.OneWayBind(ViewModel, vm => vm.StageGlyph, v => v.IconLabel.Text));
                d(this.WhenAnyValue(x => x.ViewModel.BackTranslateAssignmentCard)
                    .Subscribe(x =>
                    {
                        var source = BindableLayout.GetItemsSource(BackTranslateCard);
                        if (source == null)
                        {
                            BindableLayout.SetItemsSource(BackTranslateCard, new List<TeamAssignmentCardViewModel>{x});
                        }
                    }));
                d(this.WhenAnyValue(x => x.ViewModel.NoteTranslateAssignmentCard)
                    .Subscribe(x =>
                    {
                        var source = BindableLayout.GetItemsSource(NoteTranslateCard);
                        if (source == null)
                        {
                            BindableLayout.SetItemsSource(NoteTranslateCard, new List<TeamAssignmentCardViewModel>{x});
                        }
                    })); 
                d(this.WhenAnyValue(x => x.ViewModel.ConsultantAssignmentCard)
                    .Subscribe(x =>
                    {
                        var source = BindableLayout.GetItemsSource(ConsultantCard);
                        if (source == null)
                        {
                            BindableLayout.SetItemsSource(ConsultantCard, new List<TeamAssignmentCardViewModel>{x});
                        }
                    })); 
                d(this.WhenAnyValue(x => x.ViewModel.BackTranslate2AssignmentCard)
                    .Subscribe(x =>
                    {
                        var source = BindableLayout.GetItemsSource(BackTranslate2Card);
                        if (source == null)
                        {
                            BindableLayout.SetItemsSource(BackTranslate2Card, new List<TeamAssignmentCardViewModel>{x});
                        }
                    }));  
                d(this.WhenAnyValue(x => x.ViewModel.TranscribeAssignmentCard)
                    .Subscribe(x =>
                    {
                        var source = BindableLayout.GetItemsSource(TranscribeCard);
                        if (source == null)
                        {
                            BindableLayout.SetItemsSource(TranscribeCard, new List<TeamAssignmentCardViewModel>{x});
                        }
                    })); 
                d(this.WhenAnyValue(x => x.ViewModel.Transcribe2AssignmentCard)
                    .Subscribe(x =>
                    {
                        var source = BindableLayout.GetItemsSource(Transcribe2Card);
                        if (source == null)
                        {
                            BindableLayout.SetItemsSource(Transcribe2Card, new List<TeamAssignmentCardViewModel>{x});
                        }
                    }));
                d(this.BindCommandCustom(SettingsButtonGestureRecognizer, v => v.ViewModel.OpenStageSettingsCommand));
                d(this.OneWayBind(ViewModel, vm => vm.ShowBackTranslateCard, v => v.BT1Label.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowBackTranslateCard, v => v.BackTranslateCard.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowBackTranslate2Card, v => v.BT2Label.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowBackTranslate2Card, v => v.BackTranslate2Card.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowNoteTranslateCard, v => v.NTLabel.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowNoteTranslateCard, v => v.NoteTranslateCard.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowTranscribeCard, v => v.TranscribeCard.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowTranscribeCard, v => v.T1Label.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowTranscribe2Card, v => v.Transcribe2Card.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowTranscribe2Card, v => v.T2Label.IsVisible));
            });
        }
    }