using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.NotePlacementPlayer;
using Render.Kernel;
using Render.Resources;
using Render.TempFromVessel.Kernel;

namespace Render.Components.Navigation;

public class ItemDetailNavigationViewModel : ViewModelBase
{
    public IReadOnlyList<INavigationMarker> Markers { get; private set; }
    [Reactive] public DomainEntity CurrentItem { get; private set; }
    [Reactive] public bool HasNextItem { get; private set; }
    [Reactive] public bool HasNextRequiredItem { get; private set; }
    [Reactive] public bool HasPreviousItem { get; private set; }
    [Reactive] public bool HasPreviousRequiredItem { get; private set; }

    public ReactiveCommand<Unit, Unit> MoveToNextItemCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveToPreviousItemCommand { get; }
    public ReactiveCommand<Unit, Unit> OnBeforeChangeItemCommand { get; set; }
    public ReactiveCommand<DomainEntity, Unit> OnChangeItemCommand { get; }

    public ItemDetailNavigationViewModel(
        DomainEntity item,
        IReadOnlyList<INavigationMarker> markers,
        ReactiveCommand<DomainEntity, Unit> onChangeItemCommand,
        IViewModelContextProvider viewModelContextProvider,
        IObservable<bool> canMove=null)
        : base(string.Empty, viewModelContextProvider)
    {
        CurrentItem = item;
        Markers = markers;

        MoveToNextItemCommand = ReactiveCommand.CreateFromTask(MoveToNextItemAsync, canMove ?? Observable.Return(true));
        MoveToPreviousItemCommand = ReactiveCommand.CreateFromTask(MoveToPreviousItemAsync, canMove ?? Observable.Return(true));
        OnChangeItemCommand = onChangeItemCommand;

        Disposables.Add(this.WhenAnyValue(vm => vm.CurrentItem)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(SetNavigationState));

        Disposables.Add(this.WhenAnyValue(vm => vm.Markers.Count)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => SetNavigationState(CurrentItem)));
    }

    private async Task MoveToNextItemAsync()
    {
        if (HasNextItem == false)
        {
            return;
        }

        if (OnBeforeChangeItemCommand != null)
        {
            await OnBeforeChangeItemCommand.Execute();
        }

        var item = GetAnotherItem(CurrentItem, true, false);

        if (OnChangeItemCommand != null)
        {
            await OnChangeItemCommand.Execute(item);
        }

        CurrentItem = item;
    }

    private async Task MoveToPreviousItemAsync()
    {
        if (HasPreviousItem == false)
        {
            return;
        }

        if (OnBeforeChangeItemCommand != null)
        {
            await OnBeforeChangeItemCommand.Execute();
        }

        var item = GetAnotherItem(CurrentItem, false, false);

        if (OnChangeItemCommand != null)
        {
            await OnChangeItemCommand.Execute(item);
        }

        CurrentItem = item;
    }

    private DomainEntity GetAnotherItem(DomainEntity item, bool next, bool required)
    {
        if (Markers == null || item == null)
        {
            return null;
        }

        if (Markers.All(cm => cm.Item.Equals(item) == false))
        {
            return null;
        }

        var marker =
            (next ? Markers : Markers.Reverse())
            .SkipWhile(cm => cm.Item.Equals(item) == false)
            .Skip(1) // skip current
            .FirstOrDefault(cm => !required || cm.FlagState == FlagState.Required);

        return marker?.Item;
    }

    private bool HasAnotherConversation(DomainEntity item, bool next, bool required)
    {
        return GetAnotherItem(item, next, required) != null;
    }

    private void SetNavigationState(DomainEntity item)
    {
        HasNextItem = HasAnotherConversation(item, true, false);
        HasNextRequiredItem = HasAnotherConversation(item, true, true);
        HasPreviousItem = HasAnotherConversation(item, false, false);
        HasPreviousRequiredItem = HasAnotherConversation(item, false, true);
    }

    public override void Dispose()
    {
        CurrentItem = null;
        Markers = null;
        base.Dispose();
    }
}