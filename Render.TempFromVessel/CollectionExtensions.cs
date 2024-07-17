namespace Render.TempFromVessel
{
    public static class CollectionExtensions
    {
        public static int IndexOf<T>(this IEnumerable<T> source, T value)
        {
            return source.IndexOf(value, EqualityComparer<T>.Default);
        }

        public static int IndexOf<T>(this IEnumerable<T> source, T value, IEqualityComparer<T> comparer)
        {
            var found = source.Select((a, i) => new { a, i }).FirstOrDefault(x => comparer.Equals(x.a, value));
            return found == null ? -1 : found.i;
        }

        public static int IndexOf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            var found = source.Select((a, i) => new { a, i }).FirstOrDefault(x => predicate(x.a));
            return found == null ? -1 : found.i;
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return source.DistinctBy(keySelector, EqualityComparer<TKey>.Default);
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (keySelector == null)
            {
                throw new ArgumentNullException("keySelector");
            }
            if (comparer == null)
            {
                throw new ArgumentNullException("comparer");
            }

            var knownKeys = new HashSet<TKey>(comparer);
            foreach (TSource element in source)
            {
                if (knownKeys.Add(keySelector(element)))
                    yield return element;
            }
        }
    }
}