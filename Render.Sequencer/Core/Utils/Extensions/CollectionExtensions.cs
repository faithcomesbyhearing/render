using System.Collections;

namespace Render.Sequencer.Core.Utils.Extensions;

internal static class CollectionExtensions
{
    public static bool IsEmpty(this ICollection array)
    {
        return array.Count is 0;
    }

    public static bool IsNotEmpty(this ICollection array)
    {
        return array.Count is not 0;
    }

    public static bool IsNullOrEmpty(this IEnumerable? enumerable)
    {
        if (enumerable is not null)
        {
            return !enumerable.GetEnumerator().MoveNext();
        }

        return true;
    }

    internal static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (var item in enumerable)
        {
            action.Invoke(item);
        }
    }

    internal static T? GetPreviousItemOf<T>(this IList<T> collecetion, T item)
        where T: class
    {
        var previousItemIndex = collecetion.IndexOf(item) - 1;
        if (previousItemIndex < 0)
        {
            return null;
        }

        return collecetion[previousItemIndex];
    }

    internal static T? GetNextItemOf<T>(this IList<T> collecetion, T item)
        where T: class
    {
        var nextItemIndex = collecetion.IndexOf(item) + 1;
        if (nextItemIndex >= collecetion.Count)
        {
            return null;
        }

        return collecetion[nextItemIndex];
    }

    internal static T? GetOrDefault<T>(this IList<T> collecetion, int index)
        where T: class
    {
        if (index < 0 || index >= collecetion.Count)
        {
            return null;
        }

        return collecetion[index];
    }
}