using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Audio;
using Render.Sequencer.Core.Base;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Sequencer.Views.Toolbar.ToolbarItems;
using Render.Sequencer.Views.Toolbar.ToolbarItems.Combine;
using Render.Sequencer.Views.Toolbar.ToolbarItems.Flag;
using Render.Sequencer.Views.Toolbar.ToolbarItems.Player;
using Render.Sequencer.Views.Toolbar.ToolbarItems.Recorder;
using Render.Sequencer.Views.Toolbar.ToolbarItems.Editor;
using System.Reactive.Linq;
using Render.Sequencer.Views.Toolbar.ToolbarItems.Utils;

namespace Render.Sequencer.Views.Toolbar;

public class ToolbarViewModel : BaseViewModel
{
    private readonly bool _useFlags;

    private DeleteToolbarItemViewModel? _deleteItem;
    private UndoDeleteToolbarItemViewModel? _undoDeleteItem;
    private RecordToolbarItemViewModel? _recordItem;
    private StopToolbarItemViewModel? _stopItem;
    private AppendRecordToolbarItemViewModel? _appendRecordItem;
    private PlayToolbarItemViewModel? _playItem;
    private PauseToolbarItemViewModel? _pauseItem;
    private FlagToolbarItemViewModel? _addFlagItem;
    private UndoCombineToolbarItemViewModel? _undoCombineItem;

    private EditorAppendRecordToolbarItemViewModel? _editorAppendRecordItem;
    private EditorCutToolbarItemViewModel? _editorCutItem;
    private EditorDeleteToolbarItemViewModel? _editorDeleteItem;
    private EditorUndoToolbarItemViewModel? _editorUndoItem;

    [Reactive]
    public InternalSequencer Sequencer { get; private set; }

    [Reactive]
    public ToolbarItemsObservableCollection ToolbarItems { get; private set; }

    [Reactive]
    public bool HasAudio { get; private set; }

    [Reactive]
    public bool HasTimer { get; set; } = true;

    public ToolbarViewModel(SequencerMode mode, InternalSequencer sequencer)
    {
        _useFlags = sequencer.FlagType is not FlagType.None;

        Sequencer = sequencer;
        ToolbarItems = CreateToolbarItems(mode);

        SetupListeners();
    }

    public BaseToolbarItemViewModel AddToolbarItem(ToolbarItemModel item, int? position = null)
    {
        position ??= ToolbarItems.Count;

        var toolbarItem = new CustomToolbarItemViewModel(item);
        ToolbarItems.Insert(position.Value, toolbarItem);

        return toolbarItem;
    }

    private ToolbarItemsObservableCollection CreateToolbarItems(SequencerMode mode)
    {
        var items = new List<BaseToolbarItemViewModel>();

        switch (mode)
        {
            case SequencerMode.Player:
                FillPlayerItemsList(items);
                break;
            case SequencerMode.Recorder when Sequencer.EditorMode is false:
                FillRecorderItemsCollection(items);
                break;
            case SequencerMode.Recorder when Sequencer.EditorMode is true:
                FillEditorItemsCollection(items);
                break;
        };

        if (_useFlags)
        {
            _addFlagItem = new FlagToolbarItemViewModel(Sequencer);
            items.Add(_addFlagItem);
        }

        if (Sequencer.IsCombining)
        {
            _undoCombineItem = new UndoCombineToolbarItemViewModel(Sequencer);
            items.Add(_undoCombineItem);
        }

        return new ToolbarItemsObservableCollection(items);
    }

    private void FillPlayerItemsList(List<BaseToolbarItemViewModel> items)
    {
        _playItem = new PlayToolbarItemViewModel(Sequencer);
        _pauseItem = new PauseToolbarItemViewModel(Sequencer);

        items.Add(_playItem);
        items.Add(_pauseItem);
    }

    private void FillRecorderItemsCollection(List<BaseToolbarItemViewModel> items)
    {
        _deleteItem = new DeleteToolbarItemViewModel(Sequencer);
        _undoDeleteItem = new UndoDeleteToolbarItemViewModel(Sequencer);
        _recordItem = new RecordToolbarItemViewModel(Sequencer);
        _stopItem = new StopToolbarItemViewModel(Sequencer);
        _appendRecordItem = new AppendRecordToolbarItemViewModel(Sequencer);
        _playItem = new PlayToolbarItemViewModel(Sequencer);
        _pauseItem = new PauseToolbarItemViewModel(Sequencer);

        items.AddRange(new BaseToolbarItemViewModel[]
        {
            _deleteItem, _undoDeleteItem,
            _recordItem, _stopItem,
            _playItem, _pauseItem,
            _appendRecordItem
        });
    }

    private void FillEditorItemsCollection(List<BaseToolbarItemViewModel> items)
    {
        _editorUndoItem = new EditorUndoToolbarItemViewModel(Sequencer);
        _editorDeleteItem = new EditorDeleteToolbarItemViewModel(Sequencer);
        _recordItem = new RecordToolbarItemViewModel(Sequencer);
        _stopItem = new StopToolbarItemViewModel(Sequencer);
        _playItem = new PlayToolbarItemViewModel(Sequencer);
        _pauseItem = new PauseToolbarItemViewModel(Sequencer);
        _editorAppendRecordItem = new EditorAppendRecordToolbarItemViewModel(Sequencer);
        _editorCutItem = new EditorCutToolbarItemViewModel(Sequencer);

        items.AddRange(new BaseToolbarItemViewModel[] 
        { 
            _editorUndoItem, _editorDeleteItem, 
            _recordItem, _stopItem, 
            _playItem, _pauseItem, 
            _editorAppendRecordItem, _editorCutItem });
    }

    private void SetupListeners()
    {
        TryAddDeleteItemListeners();
        TryAddRecordPlayItemsListeners();
        TryAddEditorItemsListeners();
        TryAddFlagItemListeners();
        TryAddUndoCombineListeners();

        Sequencer
            .WhenAnyValue(sequencer => sequencer.State, sequencer => sequencer.TotalDuration)
            .Where(_ => Sequencer.IsNotRecording())
            .Subscribe(((SequencerState State, double TotalDuration) options) =>
                HasAudio = Sequencer.Audios?.Any(audio => audio.IsTempOrEmpty is false) ?? false)
            .ToDisposables(Disposables);
    }

    private void TryAddUndoCombineListeners()
    {
        if (_undoCombineItem is null)
        {
            return;
        }

        Sequencer
            .WhenAnyValue(sequencer => sequencer.State)
            .Where(_ => Sequencer.IsPlayer())
            .Subscribe(state => _undoCombineItem.SetState(
                state is not SequencerState.Playing && 
                Sequencer.Audios.Count(audio => audio.IsCombined) > 1))
            .ToDisposables(Disposables);

        Sequencer.Audios
            .ToObservableChangeSet()
            .AutoRefresh(audio => audio.IsCombined)
            .ToCollection()
            .Select(audios => audios.Count(audio => audio.IsCombined))
            .Subscribe(combinedCount => _undoCombineItem.SetState(combinedCount > 1))
            .ToDisposables(Disposables);
    }

    private void TryAddDeleteItemListeners()
    {
        if (_deleteItem is null || _undoDeleteItem is null)
        {
            return;
        }

        Sequencer
            .WhenAnyValue(
                sequencer => sequencer.State,
                sequencer => sequencer.TotalDuration,
                sequencer => sequencer.AppendRecordMode)
            .Where(_ => Sequencer.IsNotRecording())
            .Subscribe(((SequencerState _, double __, bool isAppendRecord) options) =>
            {
                var currentAudio = Sequencer.CurrentAudio;
                var isTempAudio = currentAudio.IsTemp;
                var hasAudioData = currentAudio.HasAudioData();
                var isActive = Sequencer.IsRecording() || Sequencer.IsPlaying();
                var deleteItemsState = isActive ? ToolbarItemState.Disabled : ToolbarItemState.Active;

                _deleteItem.SetState(hasAudioData && isActive is false && options.isAppendRecord is false);
                _undoDeleteItem.SetState(isTempAudio && isActive is false && options.isAppendRecord is false);
                _deleteItem.SetIsAvailable(currentAudio.Audio?.CanDelete is true && isTempAudio is false);
                _undoDeleteItem.SetIsAvailable(currentAudio.Audio?.CanDelete is true && isTempAudio);
            })
            .ToDisposables(Disposables);
    }

    private void TryAddRecordPlayItemsListeners()
    {
        if (_recordItem is not null && _stopItem is not null &&
            _playItem is not null && _pauseItem is not null)
        {
            Sequencer
                .WhenAnyValue(
                    sequencer => sequencer.CurrentAudio,
                    sequencer => sequencer.TotalDuration,
                    sequencer => sequencer.AppendRecordMode,
                    sequencer => sequencer.AllowAppendRecordMode)
                .Where(_ => Sequencer.IsRecorder() && Sequencer.IsNotRecording() && Sequencer.EditorMode is false)
                .Subscribe(((SequencerAudio Audio, double _, bool isAppendRecord, bool allowAppendRecord) options) =>
                {
                    var isTempOrEmptyAudio = options.Audio.IsTempOrEmpty;

                    _appendRecordItem?.SetIsAvailable(options.allowAppendRecord);
                    _recordItem?.SetIsAvailable(isTempOrEmptyAudio || options.isAppendRecord);
                    _stopItem?.SetIsAvailable(false);
                    _playItem?.SetIsAvailable(!isTempOrEmptyAudio && options.isAppendRecord is false);
                    _pauseItem?.SetIsAvailable(false);
                })
                .ToDisposables(Disposables);

            Sequencer
                .WhenAnyValue(sequencer => sequencer.State)
                .Where(_ => Sequencer.IsInRecorderMode())
                .Subscribe(state =>
                {
                    var isRecording = state is SequencerState.Recording;

                    _recordItem.SetIsAvailable(!isRecording);
                    _stopItem.SetIsAvailable(isRecording);
                    _playItem.SetIsAvailable(false);
                    _pauseItem.SetIsAvailable(false);
                })
                .ToDisposables(Disposables);

            Sequencer
                .WhenAnyValue(sequencer => sequencer.State)
                .Where(_ => Sequencer.IsInPlayerMode())
                .Subscribe(state =>
                {
                    var isPlaying = state is SequencerState.Playing;

                    _playItem.SetIsAvailable(!isPlaying);
                    _pauseItem.SetIsAvailable(isPlaying);
                    _recordItem.SetIsAvailable(false);
                    _stopItem.SetIsAvailable(false);
                })
                .ToDisposables(Disposables);

            return;
        }

        if (_playItem is not null && _pauseItem is not null)
        {
            Sequencer
                .WhenAnyValue(sequencer => sequencer.CurrentAudio)
                .Where(_ => Sequencer.IsPlayer())
                .Subscribe(audio => _playItem!.State = audio.HasAudioData() ? ToolbarItemState.Active : ToolbarItemState.Disabled)
                .ToDisposables(Disposables);

            Sequencer
                .WhenAnyValue(sequencer => sequencer.State)
                .Subscribe(state =>
                {
                    var isPlaying = state is SequencerState.Playing;

                    _playItem.SetIsAvailable(!isPlaying); 
                    _pauseItem.SetIsAvailable(isPlaying);
                })
                .ToDisposables(Disposables);
        }
    }

    private void TryAddEditorItemsListeners()
    {
        Sequencer
            .WhenAnyValue(
                sequencer => sequencer.State,
                sequencer => sequencer.LastDeletedAudio)
            .Where(_ => Sequencer.EditorMode)
            .Subscribe(_ =>
            {
                var hasAudio = Sequencer.CurrentAudio is not null
                    && Sequencer.CurrentAudio.HasAudioData();
                _editorUndoItem?.SetState(Sequencer.IsNotActive() && Sequencer.LastDeletedAudio is not null);
                _editorDeleteItem?.SetState(Sequencer.IsNotActive() && hasAudio);
                _editorAppendRecordItem?.SetState(Sequencer.IsNotActive() && hasAudio);
                _editorCutItem?.SetState(Sequencer.IsNotActive() && hasAudio);
            })
            .ToDisposables(Disposables);
    }

    private void TryAddFlagItemListeners()
    {
        if (_addFlagItem is null)
        {
            return;
        }

        Sequencer
            .WhenAnyValue(
                sequencer => sequencer.State,
                sequencer => sequencer.TotalDuration,
                sequencer => sequencer.AppendRecordMode)
            .Subscribe(_ =>
            {
                _addFlagItem!.State = Sequencer.State switch
                {
                    SequencerState.Initial => Sequencer.CurrentAudio.IsTempOrEmpty || Sequencer.AppendRecordMode ?
                        ToolbarItemState.Disabled :
                        ToolbarItemState.Active,

                    SequencerState.Loaded => Sequencer.CurrentAudio.IsTempOrEmpty || Sequencer.AppendRecordMode ?
                        ToolbarItemState.Disabled :
                        ToolbarItemState.Active,

                    SequencerState.Playing => ToolbarItemState.Disabled,
                    SequencerState.Recording => ToolbarItemState.Disabled,

                    _ => throw new NotImplementedException(),
                };
            })
            .ToDisposables(Disposables);
    }

    public override void Dispose()
    {
        ToolbarItems.ForEach(item => item.Dispose());
        ToolbarItems.Clear();

        base.Dispose();
    }
}