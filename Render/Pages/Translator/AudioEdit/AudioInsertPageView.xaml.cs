using System.Reactive.Linq;
using ReactiveUI;
using Render.Components.AudioRecorder;
using Render.Kernel.WrappersAndExtensions;

namespace Render.Pages.Translator.AudioEdit
{
    public partial class AudioInsertPageView
    {
        public AudioInsertPageView()
        {
            InitializeComponent();
            
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, v => v.Layout.FlowDirection));
                d(this.OneWayBind(ViewModel, vm => vm.MiniAudioRecorderViewModel, v => v.MiniRecorder.BindingContext));

                d(this.BindCommandCustom(RecordGesture, v => v.ViewModel.MiniAudioRecorderViewModel.StartRecordingCommand));
                d(this.BindCommandCustom(StopRecordGesture, v => v.ViewModel.MiniAudioRecorderViewModel.StopRecordingCommand));
                d(this.BindCommand(ViewModel, vm => vm.InsertAudioCommand, v => v.InsertAudioGesture));
                d(this.BindCommand(ViewModel, vm => vm.ClosePopupCommand, v => v.CloseGesture));

                d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.loadingView.IsVisible));

                d(this.WhenAnyValue(x => x.ViewModel.MiniAudioRecorderViewModel.AudioRecorderState)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(OnAudioRecorderStateChange));
            });
        }

        private void OnAudioRecorderStateChange(AudioRecorderState state)
        {
            switch (state)
            {
                case AudioRecorderState.TemporaryDeleted:
                case AudioRecorderState.NoAudio:
                case AudioRecorderState.CanAppendAudio:
                    RecordButton.IsVisible = true;
                    StopRecordButton.IsVisible = false;
                    SubmitMessage.IsVisible = false;
                    break;
                case AudioRecorderState.Recording:
                    RecordButton.IsVisible = false;
                    StopRecordButton.IsVisible = true;
                    SubmitMessage.IsVisible = false;
                    break;
                case AudioRecorderState.CanPlayAudio:
                case AudioRecorderState.PlayingAudio:
                    RecordButton.IsVisible = false;
                    StopRecordButton.IsVisible = false;
                    SubmitMessage.IsVisible = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}