using Couchbase.Lite;
using Render.Models.Sections;

namespace Render.Services.SyncService
{
    internal class CustomConflictResolver : IConflictResolver
    {
        public const string DeletedKey = "Deleted";
        public const string IsDeletedKey = "IsDeleted";
        public const string DateUpdatedKey = "DateUpdated";
        public const string TypeKey = "Type";

        //Seems it should be applied for all entities,
        //but there are only section references' issues at the current time
        private static readonly HashSet<string> TypesMustBeResolvedByDateUpdated
            = [typeof(SectionReferenceAudio).Name];

        public static CustomConflictResolver Instance { get; } = new CustomConflictResolver();

        private static Document ResolveByCustomDeleted(Conflict conflict)
        {
            return ResolveByCustomDeleted(conflict, DeletedKey)
                ?? ResolveByCustomDeleted(conflict, IsDeletedKey);
        }

        private static Document ResolveByCustomDeleted(Conflict conflict, string propertyKey)
        {
            if (BothHasProperty(conflict, propertyKey))
            {
                var localIsDeleted = conflict.LocalDocument?.GetBoolean(propertyKey) is true;
                var remoteIsDeleted = conflict.RemoteDocument?.GetBoolean(propertyKey) is true;
                if (localIsDeleted ^ remoteIsDeleted) //exactly one should be true
                {
                    return localIsDeleted ? conflict.LocalDocument : conflict.RemoteDocument;
                }
            }

            return null;
        }

        private static Document ResolveByDateUpdated(Conflict conflict)
        {
            if (BothHasProperty(conflict, DateUpdatedKey))
            {
                var localUpdatedAt = conflict.LocalDocument?.GetDate(DateUpdatedKey);
                var remoteUpdatedAt = conflict.RemoteDocument?.GetDate(DateUpdatedKey);
                if (localUpdatedAt.HasValue && remoteUpdatedAt.HasValue)
                {
                    return localUpdatedAt.Value >= remoteUpdatedAt
                        ? conflict.LocalDocument
                        : conflict.RemoteDocument;
                }
            }

            return null;
        }

        private static bool MustBeResolvedByDateUpdated(Conflict conflict)
        {
            if (BothHasProperty(conflict, TypeKey))
            {
                var localType = conflict.LocalDocument?.GetString(TypeKey);
                var remoteType = conflict.LocalDocument?.GetString(TypeKey);
                
                return MustBeResolvedByDateUpdated(localType)
                    && MustBeResolvedByDateUpdated(remoteType);
            }

            return false;
        }

        private static bool MustBeResolvedByDateUpdated(string type)
        {
            return !string.IsNullOrEmpty(type)
                && TypesMustBeResolvedByDateUpdated.Contains(type);
        }

        private static bool BothHasProperty(Conflict conflict, string propertyKey)
        {
            return conflict.LocalDocument?.Keys.Contains(propertyKey) is true
                && conflict.RemoteDocument?.Keys.Contains(propertyKey) is true;
        }

        public Document Resolve(Conflict conflict)
        {
            var resolvedDocument = ResolveByCustomDeleted(conflict);
            if (resolvedDocument is null
                && MustBeResolvedByDateUpdated(conflict))
            {
                resolvedDocument = ResolveByDateUpdated(conflict);
            }

            return resolvedDocument ?? ConflictResolver.Default.Resolve(conflict);
        }
    }
}
