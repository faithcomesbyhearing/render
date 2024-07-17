using System.Collections;

namespace Render.Repositories.Extensions
{
    public static class CollectionExtensions
    {
        public static bool IsNullOrEmpty(this IEnumerable @this)
        {
            if (@this != null)
            {
                return !@this.GetEnumerator().MoveNext();
            }

            return true;
        }
    }
}