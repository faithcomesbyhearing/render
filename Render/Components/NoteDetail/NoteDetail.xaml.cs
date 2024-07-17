using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using Render.Components.AudioRecorder;
using Render.Kernel.WrappersAndExtensions;

namespace Render.Components.NoteDetail
{
    public partial class NoteDetail
    {
        public NoteDetail()
        {
            InitializeComponent();
            
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, 
                    v => v.Layout.FlowDirection));
                d(this.OneWayBind(ViewModel, vm => vm.Title, v => v.Title.Text));
                d(this.Bind(ViewModel, vm => vm.TextMessage, v => v.TextMessageEditor.Text));
                d(this.BindCommandCustom(SubmitMessageGesture, v => v.ViewModel.EntryReturnCommand));
                d(this.BindCommandCustom(BackgroundGesture, v => v.ViewModel.CloseModalCommand));
                d(this.BindCommandCustom(CloseGesture, v => v.ViewModel.ForceCloseModalCommand));
                d(this.OneWayBind(ViewModel, vm => vm.AllowEditing, v => v.PopupNotesFooter.IsVisible));
                
                d(this.OneWayBind(ViewModel, vm => vm.MiniAudioRecorderViewModel, v => v.MiniRecorder.BindingContext));
                d(this.BindCommandCustom(RecordGesture, v => v.ViewModel.StartRecordingCommand));
                d(this.BindCommandCustom(StopRecordGesture, v => v.ViewModel.StopRecordingCommand));

                d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.loadingView.IsVisible));

                d(this.WhenAnyValue(x => x.ViewModel.Messages.Items)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async messages =>
                {
                    var source = BindableLayout.GetItemsSource(MessageList);
                    if (source == null)
                    {
                        BindableLayout.SetItemsSource(MessageList, messages);
                        await ScrollToBottomAsync();
                    }
                }));

                d(this.WhenAnyValue(x => x.ViewModel.Conversation)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async _ =>
                {
                    await ScrollToBottomAsync();
                }));

                d(this.WhenAnyValue(x => x.ViewModel.MiniAudioRecorderViewModel.AudioRecorderState)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(OnAudioRecorderStateChange));
               
                d(this.WhenAnyValue(x => x.ViewModel.InputState)
                    .Subscribe(SetVisibleBasedOnState));
                
                d(this.OneWayBind(ViewModel, vm => vm.NoteDetailNavigationViewModel, v => v.NavigationView.BindingContext));
                d(this.WhenAnyValue(v => v.ViewModel.NoteDetailNavigationViewModel)
                    .Subscribe(vm => NavigationView.IsVisible = vm != null));
                
            });

            StopRecordGesture.Tapped += EditingCompleted;
            SubmitMessageGesture.Tapped += EditingCompleted;
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
                    ViewModel.InputState = InputState.None;
                    break;
                case AudioRecorderState.Recording:
                    RecordButton.IsVisible = false;
                    StopRecordButton.IsVisible = true;
                    ViewModel.InputState = InputState.Audio;
                    break;
                case AudioRecorderState.CanPlayAudio:
                case AudioRecorderState.PlayingAudio:
                    ViewModel.InputState = InputState.None;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void EditingCompleted(object sender, EventArgs eventArgs)
        {
            _ = ScrollToBottomAsync(true);
        }

        private void SetVisibleBasedOnState(InputState inputState)
        {
            switch (inputState)
            {
                case InputState.Text:
                    RecordButton.IsVisible = false;
                    SubmitMessage.IsVisible = true;
                    TextMessageEditor.IsVisible = true;
                    MiniRecorder.IsVisible = false;
                    break;
                case InputState.Audio:
                    TextMessageEditor.IsVisible = false;
                    SubmitMessage.IsVisible = false;
                    MiniRecorder.IsVisible = true;
                    break;
                case InputState.None:
                    RecordButton.IsVisible = true;
                    SubmitMessage.IsVisible = false;
                    TextMessageEditor.IsVisible = true;
                    MiniRecorder.IsVisible = false;
                    break;
            }
        }

        private void TextMessageEditor_OnSizeChanged(object sender, EventArgs e)
        {
            const double maxEditorHeight = 300;
            if (sender is Editor editor && editor.Height > maxEditorHeight)
            {
                editor.HeightRequest = maxEditorHeight;
            }
        }

        private void TextMessageEditor_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is Editor editor)
            {
                //set -1 to return HeightRequest=Auto
                editor.HeightRequest = -1;
            }
        }

        private async Task ScrollToBottomAsync(bool animated = false)
        {
            await Task.Delay(500);
            await MessageScrollView.ScrollToAsync(0, MessageScrollView.Height * 10000, animated);
        }
    }
}