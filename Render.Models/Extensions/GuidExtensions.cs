namespace Render.Models.Extensions
{
    public static class GuidExtensions
    {
        public static Guid Merge(this IEnumerable<Guid> items)
        {
            var ordered = items.OrderBy(a => a).ToArray();
            if (ordered.Length is 0)
            {
                return Guid.Empty;
            }

            var result = ordered[0];
            for (int i = 1; i < ordered.Length; i++)
            {
                result = Merge(result, ordered[i]);
            }

            return result;
        }

        public static Guid Merge(this Guid first, Guid second)
        {
            var firstBytes = first.ToByteArray();
            var secondBytes = second.ToByteArray();
            var targetBytes = new byte[firstBytes.Length];

            for (var i = 0; i < targetBytes.Length; i++)
            {
                targetBytes[i] = (byte)(firstBytes[i] ^ secondBytes[i]);
            }

            return new Guid(targetBytes);
        }
    }
}
