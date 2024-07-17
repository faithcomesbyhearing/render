using Render.Sequencer.Contracts.ToolbarItems;

namespace Render.Sequencer.Views.Toolbar.ToolbarItems.Utils;

public static class ToolbarItemsObservableCollectionExtensions
{
    public static IPlayToolbarItem? GetPlayItem(this ToolbarItemsObservableCollection collection)
    {
        return collection.GetItem<IPlayToolbarItem>();
    }

    public static IRecordToolbarItem? GetRecordItem(this ToolbarItemsObservableCollection collection)
    {
        return collection.GetItem<IRecordToolbarItem>();
    }

    public static IDeleteToolbarItem? GetDeleteItem(this ToolbarItemsObservableCollection collection)
    {
        return collection.GetItem<IDeleteToolbarItem>();
    }

    public static IFlagToolbarItem? GetFlagItem(this ToolbarItemsObservableCollection collection)
    {
        return collection.GetItem<IFlagToolbarItem>();
    }
}