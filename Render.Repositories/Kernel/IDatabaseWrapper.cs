using Couchbase.Lite;
using Couchbase.Lite.Query;

namespace Render.Repositories.Kernel
{
    public interface IDatabaseWrapper : IDisposable
    {
        IDataSourceAs GetDataSource();

        void Save(MutableDocument document);

        void Delete(Document document);

        Document GetDocument(string key);

        void CompactDatabase();

        void Purge(Document document);

        void InBatch(Action action);
        
        BatchStatus BatchInsertSynchronous(List<MutableDocument> list);

        BatchStatus BatchDeleteSynchronous(List<Document> documentList);

        BatchStatus BatchUpdateSynchronous(IExpression updateCriteria, Action<MutableDocument> updateAction);
    }
}