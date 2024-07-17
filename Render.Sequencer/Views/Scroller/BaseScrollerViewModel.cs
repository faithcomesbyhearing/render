using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Audio;
using Render.Sequencer.Core.Base;
using Render.Sequencer.Core.Painters;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Sequencer.Core.Utils.Helpers;
using Render.Sequencer.Views.Flags.Base;
using Render.Sequencer.Views.WaveForm.MiniItems;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows.Input;

namespace Render.Sequencer.Views.Scroller;

public abstract class BaseScrollerViewModel : BaseViewModel
{
    [Reactive]
    public InternalSequencer Sequencer { get; private set; }

    [Reactive]
    public BaseWaveformPainter Painter { get; private set; }

    /// <summary>
    /// Width of the draggable rectangle
    /// </summary>
    [Reactive]
    public double ScrollerWidth { get; set; }

    /// <summary>
    /// Overall width of the ScrollerView
    /// </summary>
    [Reactive]
    public double Width { get; set; }

    [Reactive]
    public double ScrollerTranslationX { get; set; }

    [Reactive]
    public bool HasScroller { get; set; }

    [Reactive]
    public bool HasScrubber { get; set; }

    public ObservableCollection<BaseMiniWaveFormItemViewModel> MiniWaveFormItems { get; set; }

    private ReadOnlyObservableCollection<BaseFlagViewModel>? _flags;
    public ReadOnlyObservableCollection<BaseFlagViewModel> Flags
    {
        get => _flags!;
    }

    public BaseScrollerViewModel(InternalSequencer sequencer)
    {
        Sequencer = sequencer;
        HasScroller = true;
        MiniWaveFormItems = new();
        Painter = new RecorderMiniWaveformPainter();

        SetupListeners();
    }

    protected abstract BaseMiniWaveFormItemViewModel CreateMiniWaveFormViewModel(SequencerAudio audio);

    protected virtual void SetupListeners()
    {
        this
            .WhenAnyValue(viewModel => viewModel.HasScroller)
            .Where(hasScroller => hasScroller && Sequencer.IsNotRecording())
            .Subscribe(_ => UpdateScrollerGeometry((Sequencer.InputScrollX, Sequencer.WidthRatio, Width)))
            .ToDisposables(Disposables);

        this
            .WhenAnyValue(
                viewModel => viewModel.Sequencer.InputScrollX,
                viewModel => viewModel.Sequencer.WidthRatio,
                viewModel => viewModel.Width)
            .Where(_ => Sequencer.IsNotRecording())
            .Where(((double _, double Ratio, double Width) options) => options.Width > 0 || options.Ratio > 0)
            .Subscribe(options =>
            {
                if (Sequencer.IsInAppendRecorderMode())
                {
                    UpdateRecordingScrollerGeometry(Sequencer.TotalDuration);
                }
                else
                {
                    UpdateScrollerGeometry(options);
                }
            })
            .ToDisposables(Disposables);

        this
            .WhenAnyValue(viewModel => viewModel.Sequencer.TotalDuration)
            .Subscribe(duration => HasScroller = duration is not 0)
            .ToDisposables(Disposables);

        Sequencer.Audios
            .ToObservableChangeSet()
            .Where(_ => Sequencer.IsNotRecording())
            .Transform(audio => audio)
            .Subscribe(ReplaceMiniWaveForms)
            .ToDisposables(Disposables);

        Sequencer
            .WhenAnyValue(sequencer => sequencer.TotalDuration)
            .Where(_ => Sequencer.IsRecording())
            .Subscribe(UpdateRecordingScrollerGeometry)
            .ToDisposables(Disposables);

        Sequencer
            .WhenRecordingStarted()
            .Subscribe(_ =>
            {
                HasScroller = true;

                if (Sequencer.IsInAppendRecorderMode())
                {
                    return;
                }

                ScrollerTranslationX = 0;
                ScrollerWidth = Width;
            })
            .ToDisposables(Disposables);

        Sequencer
            .WhenAppendRecordModeOn()
            .Subscribe(_ => UpdateRecordingScrollerGeometry(Sequencer.TotalDuration))
            .ToDisposables(Disposables);
    }

    protected virtual void ReplaceMiniWaveForms(IChangeSet<SequencerAudio> audiosChange)
    {
        var audios = audiosChange
            .Where(change => change.Reason is ListChangeReason.AddRange)
            .SelectMany(change => change.Range.ToArray())
            .ToArray();
     
        MiniWaveFormItems.ForEach(item => item.Dispose());
        MiniWaveFormItems.Clear();

        if (Sequencer.IsPlayer() && audios.IsEmptyAudios())
        {
            return;
        }

        audios.ForEach(audio => MiniWaveFormItems.Add(CreateMiniWaveFormViewModel(audio)));
    }

    private void UpdateScrollerGeometry((double InputScrollX, double Ratio, double Width) options)
    {
        var width = options.Width;
        var widthRatio = options.Ratio;
        var inputScrollX = options.InputScrollX;

        ScrollerWidth = width * widthRatio;

        var scrollerTranslationX = inputScrollX * widthRatio - 0.5 * ScrollerWidth;
        var maxScrollerEndX = width - ScrollerWidth;

        if (scrollerTranslationX > maxScrollerEndX)
        {
            scrollerTranslationX = maxScrollerEndX;
        }

        ScrollerTranslationX = scrollerTranslationX > 0 ? scrollerTranslationX : 0;
    }

    private void UpdateRecordingScrollerGeometry(double duration)
    {
        var secondsOnScreen = Sequencer.SecondsOnScreen;
        if (duration > secondsOnScreen)
        {
            var widthRatio = secondsOnScreen / duration;

            ScrollerWidth = widthRatio * Width;
            ScrollerTranslationX = Width - ScrollerWidth;
            Sequencer.InputScrollX = ScrollerTranslationX;
        }
        else
        {
            ScrollerWidth = Width;
            Sequencer.InputScrollX = 0;
        }
    }
}