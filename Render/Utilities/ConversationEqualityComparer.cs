using Render.Models.Sections;
using System.Diagnostics.CodeAnalysis;

namespace Render.Utilities
{
    public class ConversationEqualityComparer : IEqualityComparer<Conversation>
    {
        public bool Equals(Conversation x, Conversation y)
        {
            return x.Id.Equals(y.Id);
        }

        public int GetHashCode([DisallowNull] Conversation obj)
        {
            return obj.GetHashCode();
        }
    }
}
