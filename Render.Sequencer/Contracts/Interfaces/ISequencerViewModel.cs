using ReactiveUI;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Contracts.ToolbarItems;
using System.Reactive;

namespace Render.Sequencer.Contracts.Interfaces;

public interface ISequencerViewModel : IDisposable
{
    bool IsRightToLeftDirection { get; set; }

    double TotalDuration { get; }
    SequencerState State { get; }

    ReactiveCommand<bool?, Unit> StopCommand { get; }
    ReactiveCommand<IFlag, bool>? AddFlagCommand { get; set; }
    ReactiveCommand<IFlag, Unit>? TapFlagCommand { get; set; }
    ReactiveCommand<Unit, Unit>? LoadedCommand { get; set; }

    void AddFlag(FlagModel flag);

    IFlag? GetFlag(Guid key);

    void RemoveFlag(IFlag flag);

    IToolbarItem AddToolbarItem(ToolbarItemModel item, int? position = null);

    TToolbarItem? GetToolbarItem<TToolbarItem>() where TToolbarItem : class, IToolbarItem;
    
    void RemoveToolbarItem(IToolbarItem item);

    void AddToolbarItemAfter(IToolbarItem precedingItem, IToolbarItem newItem);

    bool TrySelectAudio(int index);
}
