using System.Reactive;
using ReactiveUI;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Contracts.ToolbarItems;
using Render.Sequencer.Views.WaveForm;
using Render.Sequencer.Views.Scroller;
using Render.Sequencer.Views.Toolbar;

namespace Render.Sequencer.Contracts.Interfaces;

public interface ISequencerViewModel : IDisposable
{
    bool IsRightToLeftDirection { get; set; }

    double TotalDuration { get; }
    SequencerState State { get; }

	BaseWaveFormViewModel? WaveFormViewModel { get;  }
	BaseScrollerViewModel? ScrollerViewModel { get; }
	ToolbarViewModel ToolbarViewModel { get; }

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
