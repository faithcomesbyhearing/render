using ReactiveUI;
using System.Reactive;
using ReactiveUI.Fody.Helpers;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Contracts.ToolbarItems;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Sequencer.Views.Scroller;
using Render.Sequencer.Views.Toolbar;
using Render.Sequencer.Views.Toolbar.ToolbarItems;
using Render.Sequencer.Views.WaveForm;
using Render.Services.AudioPlugins.AudioPlayer;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;

namespace Render.Sequencer.ViewModels;

internal abstract class BaseSequencerViewModel<TWaveFormViewModel, TScrollerViewModel> : ReactiveObject, ISequencerViewModel
    where TWaveFormViewModel : BaseWaveFormViewModel
    where TScrollerViewModel : BaseScrollerViewModel
{
    protected InternalSequencer Sequencer { get; private set; }

    protected List<IDisposable> Disposables { get; private set; }

    public ReactiveCommand<bool?, Unit> StopCommand { get; private set; }

    public ReactiveCommand<IFlag, bool>? AddFlagCommand
    {
        get => Sequencer.AddFlagCommand;
        set => Sequencer.AddFlagCommand = value;
    }

    public ReactiveCommand<IFlag, Unit>? TapFlagCommand
    {
        get => Sequencer.TapFlagCommand;
        set => Sequencer.TapFlagCommand = value;
    }

    public ReactiveCommand<Unit, Unit>? LoadedCommand { get; set; }

    public bool IsRightToLeftDirection 
    { 
        get => Sequencer.IsRightToLeftDirection; 
        set => Sequencer.IsRightToLeftDirection = value; 
    }

    public double TotalDuration
    {
        get => Sequencer.TotalDuration;
    }

    [Reactive]
    public SequencerState State { get; private set; }

    public TWaveFormViewModel WaveFormViewModel { get; protected set; }
    public TScrollerViewModel ScrollerViewModel { get; protected set; }
    public ToolbarViewModel ToolbarViewModel { get; protected set; }

    protected BaseSequencerViewModel(
        SequencerMode mode,
        Func<IAudioPlayer> playerFactory,
        Func<IAudioRecorder>? recorderFactory,
        FlagType flagType,
        bool isCombining = false,
        bool isEditor = false)
    {
        Disposables = new();
        Sequencer = new InternalSequencer(mode, flagType, playerFactory, recorderFactory, isCombining, isEditor);
        ToolbarViewModel = new ToolbarViewModel(mode, Sequencer);

        StopCommand = ReactiveCommand.CreateFromTask<bool?>(StopAsync);

        SetupListeners();
    }

    public IToolbarItem AddToolbarItem(ToolbarItemModel item, int? position = null)
    {
        return ToolbarViewModel.AddToolbarItem(item, position);
    }

    public void AddToolbarItemAfter<TToolbarItem>(IToolbarItem itemToInsert) where TToolbarItem : class, IToolbarItem
    {
        if (itemToInsert is BaseToolbarItemViewModel toolbarItem)
        {
            ToolbarViewModel.ToolbarItems.AddToolbarItemAfter<TToolbarItem>(toolbarItem);
        }
    }

    public TToolbarItem? GetToolbarItem<TToolbarItem>() where TToolbarItem : class, IToolbarItem
    {
        return ToolbarViewModel.ToolbarItems.GetItem<TToolbarItem>();
    }

    public void RemoveToolbarItem(IToolbarItem item)
    {
        ToolbarViewModel.ToolbarItems.RemoveItem(item);
    }

    public void AddToolbarItemAfter(IToolbarItem precedingItem, IToolbarItem newItem)
    {
        var itemIndex = ToolbarViewModel.ToolbarItems.IndexOf(precedingItem);
        ToolbarViewModel.ToolbarItems.Insert(itemIndex + 1, newItem);
    }

    public void AddFlag(FlagModel flag)
    {
        if (IsFlagExists(flag.Key))
        {
            return;
        }

        Sequencer.AddFlag(flag);
    }

    public IFlag? GetFlag(Guid key)
    {
        return Sequencer.GetFlag(key);
    }

    public void RemoveFlag(IFlag flag)
    {
        Sequencer.RemoveFlag(flag);
    }

    public bool TrySelectAudio(int index)
    {
        return WaveFormViewModel.TrySelectWaveForm(index);
    }

    protected virtual void SetupListeners()
    {
        this
            .WhenAnyValue(viewModel => viewModel.Sequencer.State)
            .BindTo(this, vm => vm.State)
            .ToDisposables(Disposables);
    }

    private async Task StopAsync(bool? pause = true)
    {
        if (Sequencer.IsPlaying())
        {
            if (pause is false)
            {
                Sequencer.InternalPlayer.Stop();
            }
            else
            {
                Sequencer.InternalPlayer.Pause();
            }
        }

        if (Sequencer.IsRecording())
        {
            await Sequencer.InternalRecorder.StopAsync();
        }
    }

    internal bool IsFlagExists(Guid key)
    {
        return Sequencer.IsFlagExists(key);
    }

    public virtual void Dispose()
    {
        WaveFormViewModel?.Dispose();
        WaveFormViewModel = null!;

        ToolbarViewModel?.Dispose();
        ToolbarViewModel = null!;

        ScrollerViewModel?.Dispose();
        ScrollerViewModel = null!;

        Disposables?.ForEach(disposable => disposable.Dispose());
        Disposables?.Clear();
        Disposables = null!;

        Sequencer?.Dispose();
        Sequencer = null!;
    }
}