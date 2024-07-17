using Render.Sequencer.Contracts.ToolbarItems;
using System.Collections.ObjectModel;

namespace Render.Sequencer.Views.Toolbar.ToolbarItems.Utils;

public class ToolbarItemsObservableCollection : ObservableCollection<IToolbarItem>
{
    public ToolbarItemsObservableCollection(IEnumerable<BaseToolbarItemViewModel> items)
        : base(items) { }

    public bool TryMoveItem(BaseToolbarItemViewModel? item, int targetIndex)
    {
        if (this.FirstOrDefault(item) is not BaseToolbarItemViewModel targetItem)
        {
            return false;
        }

        targetIndex = targetIndex < 0 ? 0 : targetIndex;
        var currentIndex = IndexOf(targetItem);
        if (currentIndex == targetIndex)
        {
            return false;
        }

        targetIndex = targetIndex > Count - 1 ? Count - 1 : targetIndex;
        MoveItem(currentIndex, targetIndex);

        return true;
    }

    public void AddToolbarItemAfter<TToolbarItem>(BaseToolbarItemViewModel itemToInsert)
        where TToolbarItem : class, IToolbarItem
    {
        if (GetItem<TToolbarItem>() is not BaseToolbarItemViewModel item)
        {
            return;
        }

        var anchorItemIndex = IndexOf(item);
        Insert(anchorItemIndex + 1, itemToInsert);
    }

    public TToolbarItem? GetItem<TToolbarItem>() where TToolbarItem : class, IToolbarItem
    {
        return this.FirstOrDefault(item => item is TToolbarItem) as TToolbarItem;
    }

    public void RemoveItem(IToolbarItem item)
    {
        Remove(item);
    }
}