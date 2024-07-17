using DynamicData;

namespace Render.Kernel.WrappersAndExtensions
{
    public static class DisposableExtensions
    {
        public static void DisposeSourceList<T>(this SourceList<T> sourceList) where T : IDisposable
        {
            if (sourceList == null) 
            { 
                return; 
            }

            sourceList.Dispose();

            foreach (var item in sourceList.Items)
            {
                item.Dispose();
            }
            
            sourceList.Clear();
        }

        public static void DisposeSourceCache<T, TKey>(this SourceCache<T, TKey> sourceCache) where T : IDisposable
        {
            if (sourceCache == null)
            {
                return;
            }

            sourceCache.Dispose();

            foreach (var item in sourceCache.Items)
            {
                item.Dispose();
            }

            sourceCache.Clear();
        }

        public static void DisposeCollection<T>(this ICollection<T> collection) where T : IDisposable
        {
            if (collection == null) return;

            foreach (var item in collection)
            {
                item.Dispose();
            }
        }
    }
}
