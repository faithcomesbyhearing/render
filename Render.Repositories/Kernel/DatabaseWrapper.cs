using Couchbase.Lite;
using Couchbase.Lite.Query;
using Render.Interfaces;
using Render.TempFromVessel.Document_Extensions;

namespace Render.Repositories.Kernel
{
    public class DatabaseWrapper : IDatabaseWrapper
    {
        private readonly IRenderLogger _logger;

        private Database Database { get; }
        private SemaphoreSlim SemaphoreSlim { get; }

        public DatabaseWrapper(IRenderLogger logger, string databaseName, string databasePath)
        {
            _logger = logger;

            Settings.PropertyNameConverter = new CustomPropertyNameConverter();
            var databaseConfiguration = new DatabaseConfiguration 
            {
                Directory = databasePath,
            };
            Database = new Database(databaseName, databaseConfiguration);
            CreateIndexes(databaseName);
            SemaphoreSlim = new SemaphoreSlim(1);
        }

        public IDataSourceAs GetDataSource()
        {
            return DataSource.Collection(Database.GetDefaultCollection());
        }

        public void Save(MutableDocument document)
        {
            SemaphoreSlim.Wait();
            try
            {
                Database.GetDefaultCollection().Save(document);
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }
        
        public void Delete(Document document)
        {
            SemaphoreSlim.Wait();
            try
            {
                Database.GetDefaultCollection().Delete(document);
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        public Document GetDocument(string key)
        {
            return Database?.GetDefaultCollection().GetDocument(key);
        }

        public void CompactDatabase()
        {
            SemaphoreSlim.Wait();
            try
            {
                Database.PerformMaintenance(MaintenanceType.Compact);
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        public void Purge(Document document)
        {
            SemaphoreSlim.Wait();
            try
            {
                Database.GetDefaultCollection().Purge(document);
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }
        
        public BatchStatus BatchInsertSynchronous(List<MutableDocument> list)
        {
            SemaphoreSlim.Wait();
            try
            {
                Database.InBatch(() =>
                {
                    foreach (var mutableDocument in list)
                    {
                        Database.GetDefaultCollection().Save(mutableDocument);
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e);
                return BatchStatus.Failed;
            }
            finally
            {
                SemaphoreSlim.Release();
            }
            return BatchStatus.Succeeded;
        }
        
        public BatchStatus BatchUpdateSynchronous(IExpression updateCriteria, Action<MutableDocument> updateAction)
        {
            SemaphoreSlim.Wait();

            try
            {
                Database.InBatch(() =>
                {
                    var result = QueryBuilder
                        .Select(SelectResult.Expression(Meta.ID))
                        .From(GetDataSource())
                        .Where(updateCriteria)
                        .Execute();

                    foreach (var row in result)
                    {
                        string documentId = row.GetString(0);
                        MutableDocument document = Database.GetDefaultCollection().GetDocument(documentId).ToMutable();

                        // Update the document with the new values
                        updateAction(document);

                        Database.GetDefaultCollection().Save(document);
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e);
                return BatchStatus.Failed;
            }
            finally
            {
                SemaphoreSlim.Release();
            }
            return BatchStatus.Succeeded;
        }
        
        public void InBatch(Action action)
        {
            SemaphoreSlim.Wait();
            try
            {
                Database.InBatch(action);
            }
            catch (Exception e)
            {
                _logger.LogError(e);
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }
        
        public BatchStatus BatchDeleteSynchronous(List<Document> documentList)
        {
            SemaphoreSlim.Wait();
            try
            {
                Database.InBatch(() =>
                {
                    foreach (var document in documentList)
                    {
                        Database.GetDefaultCollection().Delete(document);
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e);
                return BatchStatus.Failed;
            }
            finally
            {
                SemaphoreSlim.Release();
            }
            return BatchStatus.Succeeded;
        }
        
        private void CreateIndexes(string databaseName)
        {
            switch (databaseName)
            {
                case "render":
                    Database.GetDefaultCollection().CreateIndex(
                        "idx_Type",
                        IndexBuilder.ValueIndex(ValueIndexItem.Expression(Expression.Property("Type"))));
                    Database.GetDefaultCollection().CreateIndex(
                        "idx_Username",
                        IndexBuilder.ValueIndex(ValueIndexItem.Expression(Expression.Property("Username"))));
                    Database.GetDefaultCollection().CreateIndex(
                        "idx_ProjectId",
                        IndexBuilder.ValueIndex(ValueIndexItem.Expression(Expression.Property("ProjectId"))));
                    Database.GetDefaultCollection().CreateIndex(
                        "idx_Id",
                        IndexBuilder.ValueIndex(ValueIndexItem.Expression(Expression.Property("Id"))));
                    break;
                case "renderaudio":
                    Database.GetDefaultCollection().CreateIndex(
                        "idx_Type",
                        IndexBuilder.ValueIndex(ValueIndexItem.Expression(Expression.Property("Type"))));
                    Database.GetDefaultCollection().CreateIndex(
                        "idx_ProjectId",
                        IndexBuilder.ValueIndex(ValueIndexItem.Expression(Expression.Property("ProjectId"))));
                    Database.GetDefaultCollection().CreateIndex(
                        "idx_ParentId",
                        IndexBuilder.ValueIndex(ValueIndexItem.Expression(Expression.Property("ParentId"))));
                    Database.GetDefaultCollection().CreateIndex(
                        "idx_ParentAudioId",
                        IndexBuilder.ValueIndex(ValueIndexItem.Expression(Expression.Property("ParentAudioId"))));
                    break;
                case "localonlydata":
                    Database.GetDefaultCollection().CreateIndex(
                        "idx_UserId",
                        IndexBuilder.ValueIndex(ValueIndexItem.Expression(Expression.Property("UserId"))));
                    Database.GetDefaultCollection().CreateIndex(
                        "idx_ProjectId",
                        IndexBuilder.ValueIndex(ValueIndexItem.Expression(Expression.Property("ProjectId"))));
                    break;
            }
        }

        public void Dispose()
        {
            Database?.Dispose();
            SemaphoreSlim?.Dispose();
        }
    }
}