using System.Reactive.Linq;
using ReactiveUI;
using Render.Components.AudioRecorder;
using Render.Kernel.CustomRenderer;
using Render.Kernel.WrappersAndExtensions;
using Render.Resources;

namespace Render.Components.CommunityTestFlagLibraryModal;

public partial class CommunityTestFlagLibraryModal
{
    private Color _transparent;
    private Color _white;
    private Color _lightGray;

    public CommunityTestFlagLibraryModal()
    {
        InitializeComponent();
        _transparent = ResourceExtensions.GetColor("Transparent");
        _white = ResourceExtensions.GetColor("SecondaryText");
        _lightGray = ResourceExtensions.GetColor("AlternateBackground");

        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection,
                v => v.TopElementFrame.FlowDirection));
            d(this.WhenAnyValue(v => v.ViewModel.FlowDirection)
                .Subscribe(flowDirection =>
                {
                    var borderRadius = flowDirection == FlowDirection.LeftToRight
                        ? new CornerRadius(16, 16, 16, 0)
                        : new CornerRadius(16, 16, 0, 16);

                    ContentPanel.SetValue(Panel.BorderRadiusProperty, borderRadius);
                }));

            d(this.BindCommandCustom(CloseGesture, v => v.ViewModel.ForceCloseModalCommand));
            d(this.BindCommandCustom(FrameCloseGesture, v => v.ViewModel.CloseModalCommand));
            d(this.BindCommandCustom(BackgroundGesture, v => v.ViewModel.CloseModalCommand));

            d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.loadingView.IsVisible));

            d(this.BindCommandCustom(StartRecordGesture, v => v.ViewModel.StartRecordingCommand));
            d(this.BindCommandCustom(StopRecordGesture, v => v.ViewModel.StopRecordingCommand));

            d(this.OneWayBind(ViewModel, vm => vm.PressArrowToAddQuestionLabelText,
                v => v.PressArrowToAddQuestionLabel.Text));
            d(this.OneWayBind(ViewModel, vm => vm.PressCircleToRecordOrPressArrowToAddQuestionLabelText,
                v => v.PressCircleToRecordOrPressArrowToAddQuestionLabel.Text));

            d(this.WhenAnyValue(v => v.ViewModel.MiniAudioRecorderViewModel)
                .Subscribe(x => MiniRecorder.BindingContext = x));

            d(this.WhenAnyValue(x => x.ViewModel.MiniAudioRecorderViewModel.AudioRecorderState)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(OnAudioRecorderStateChange));

            d(this.WhenAnyValue(x => x.ViewModel.StandardQuestionCards)
                .Subscribe(i =>
                {
                    var source = BindableLayout.GetItemsSource(LibraryQuestionsPanel);

                    if (source == null)
                    {
                        BindableLayout.SetItemsSource(LibraryQuestionsPanel, i);
                    }

                    LibraryScrollView.ScrollToAsync(0, LibraryScrollView.Height * 10000, false);
                }));

            d(this.WhenAnyValue(x => x.ViewModel.QuestionCards)
                .Subscribe(i =>
                {
                    var source = BindableLayout.GetItemsSource(QuestionsPanel);

                    if (source == null)
                    {
                        BindableLayout.SetItemsSource(QuestionsPanel, i);
                    }

                    QuestionScrollView.ScrollToAsync(0, QuestionScrollView.Height * 10000, false);
                }));

            d(this.WhenAnyValue(x => x.ViewModel.IsFlagQuestionsExists,
                    x => x.ViewModel.IsLibraryQuestionsExists)
                .Subscribe(((bool IsFlagQuestionsExists, bool IsLibraryQuestionsExists) options) =>
                {
                    PressCircleToRecordLabel.IsVisible = !options.IsFlagQuestionsExists && !options.IsLibraryQuestionsExists;
                    PressCircleToRecordOrPressArrowToAddQuestionLabel.IsVisible = !options.IsFlagQuestionsExists && options.IsLibraryQuestionsExists;
                    YouHaveNoQuestionsInTheLibraryLabel.IsVisible = !options.IsLibraryQuestionsExists && !options.IsFlagQuestionsExists;
                    PressArrowToAddQuestionLabel.IsVisible = !options.IsLibraryQuestionsExists && options.IsFlagQuestionsExists;
                }));
        });
    }

    /// <summary>
    /// Library popup must not allow audio playback from the recorder component.
    /// Library popup uploads audio to the question list instead, right away after recording is stopped.
    /// Threrefore, we need to set default visual state for recorder in CanPlayAudio, PlayingAudio states.
    /// See details here: https://dev.azure.com/FCBH/Software%20Development/_workitems/edit/24234
    /// </summary>
    private void OnAudioRecorderStateChange(AudioRecorderState state)
    {
        switch (state)
        {
            case AudioRecorderState.NoAudio:
            case AudioRecorderState.CanAppendAudio:
                SetDefaultRecorderVisualState();
                break;
            case AudioRecorderState.Recording:
                SetInProgressRecorderVisualState();
                break;
            case AudioRecorderState.CanPlayAudio:
            case AudioRecorderState.PlayingAudio:
                SetDefaultRecorderVisualState();
                QuestionScrollView.ScrollToAsync(0, QuestionScrollView.Height * 10000, true);
                break;
        }
    }

    private void SetDefaultRecorderVisualState()
    {
        StartRecordingButton.IsVisible = true;
        StopRecordingButton.IsVisible = false;
        InnerRecordQuestionPanel.BackgroundColor = _transparent;
        RecordingQuestionPanel.BackgroundColor = _transparent;
        MiniRecorder.BackgroundColor = _transparent;
        MiniRecorder.ContainerFrameBorderColor = _transparent;
    }

    private void SetInProgressRecorderVisualState()
    {
        StartRecordingButton.IsVisible = false;
        StopRecordingButton.IsVisible = true;
        PressCircleToRecordLabel.IsVisible = false;
        PressCircleToRecordOrPressArrowToAddQuestionLabel.IsVisible = false;
        InnerRecordQuestionPanel.BackgroundColor = _white;
        RecordingQuestionPanel.BackgroundColor = _lightGray;
        MiniRecorder.BackgroundColor = _white;
        MiniRecorder.ContainerFrameBorderColor = _white;
    }
}