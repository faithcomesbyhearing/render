using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Snapshot;
using Render.Models.Workflow.Stage;
using Render.Resources.Localization;

namespace Render.Pages.Settings.SectionStatus.Recovery;

public class SnapshotCardViewModel : ViewModelBase
{
    private Func<Guid, bool, Task> _restoreSnapshotConfirmationCallback;
    private Func<Snapshot, Task> _snapshotSelectedCallback;

    public Stage Stage { get; private set; }
    public bool First { get; }
    public bool Last { get; }
    [Reactive] public string SnapshotDateLabel { get; set; }

    [Reactive] public Snapshot Snapshot { get; set; }
    [Reactive] public bool ShowDescription { get; set; }
    [Reactive] public bool CurrentSnapshot { get; set; }
    [Reactive] public bool IsSelected { get; set; }
    [Reactive] public SnapshotCardState SnapshotCardState { get; set; }
    [Reactive] public int StageNameColumnSpan { get; set; } = 1;

	public ReactiveCommand<Unit, Unit> SelectSnapshotCommand;
    public ReactiveCommand<Unit, Unit> RestoreSnapshotCommand;

    public SnapshotCardViewModel(
        IViewModelContextProvider viewModelContextProvider,
        Stage stage,
        bool first,
        bool last,
        bool currentSnapshot,
        Func<Guid, bool, Task> restoreSnapshotConfirmationCallback,
        Func<Snapshot, Task> snapshotSelectedCallback,
        Snapshot snapshot = null) : base("SnapshotCard", viewModelContextProvider)
    {
        _restoreSnapshotConfirmationCallback = restoreSnapshotConfirmationCallback;
        _snapshotSelectedCallback = snapshotSelectedCallback;

        if (snapshot == null)
        {
            SnapshotCardState = SnapshotCardState.Empty;
            SnapshotDateLabel = AppResources.Snapshot;
        }
        else
        {
            SnapshotCardState = SnapshotCardState.Unselected;
            Snapshot = snapshot;
        }

        Stage = stage;
        First = first;
        Last = last;
        CurrentSnapshot = currentSnapshot;
        ShowDescription = (stage.IsRemoved || stage.IsCompleteWork) && snapshot != null;

        Disposables.Add(this.WhenAnyValue(vm => vm.IsSelected)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(isSelected =>
            {
                SnapshotCardState = isSelected ? SnapshotCardState.Selected : SnapshotCardState.Unselected;
                SnapshotDateLabel = isSelected 
                    ? Snapshot?.DateUpdated.ToString("MMM dd, yyyy", AppResources.Culture) 
                    : Snapshot?.DateUpdated.ToString("MMMM dd, yyyy", AppResources.Culture);
            }));

        Disposables.Add(this.WhenAnyValue(vm => vm.Snapshot)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x =>
            {
                if (x == null)
                {
                    SnapshotCardState = SnapshotCardState.Empty;
                    SnapshotDateLabel = AppResources.Snapshot;
                }
            }));

        Disposables.Add(this.WhenAnyValue(vm => vm.ShowDescription)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x =>
            {
                StageNameColumnSpan = x ? 1 : 3;
			}));

        SelectSnapshotCommand = ReactiveCommand.Create(SelectSnapshot);
        RestoreSnapshotCommand = ReactiveCommand.Create(RestoreSnapshot);
        Disposables.Add(RestoreSnapshotCommand.IsExecuting
            .Subscribe(isExecuting => { IsLoading = isExecuting; }));
    }

    private void SelectSnapshot()
    {
        if (Snapshot != null)
        {
            IsSelected = true;

            _snapshotSelectedCallback?.Invoke(Snapshot);
        }
    }

    private void RestoreSnapshot()
    {
        _restoreSnapshotConfirmationCallback?.Invoke(Snapshot.Id, true);
    }

    public override void Dispose()
    {
        _restoreSnapshotConfirmationCallback = null;
        _snapshotSelectedCallback = null;

        Stage = null;
        Snapshot = null;

        RestoreSnapshotCommand?.Dispose();
        RestoreSnapshotCommand = null;
        SelectSnapshotCommand?.Dispose();
        SelectSnapshotCommand = null;

        base.Dispose();
    }
}

public enum SnapshotCardState
{
    Selected,
    Unselected,
    Empty
}