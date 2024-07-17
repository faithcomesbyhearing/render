using System.Diagnostics;
using Couchbase.Lite;
using Couchbase.Lite.Query;
using Render.TempFromVessel;
using Render.TempFromVessel.Document_Extensions;
using Render.TempFromVessel.Document_Extensions.Converters;
using Render.TempFromVessel.Kernel;
using Render.Repositories.Extensions;
using Render.Models.Audio;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace Render.Repositories.Kernel
{
    public enum BatchStatus
    {
        Building,
        Processing,
        ReTrying,
        Succeeded,
        Failed
    }

    /// <summary>
    /// Set up local Couchbase
    /// </summary>
    /// <typeparam name="T">Object type DDDEntity</typeparam>
    /// <seealso cref="IDataPersistence{T}" />
    public class CouchbaseLocal<T> : IDataPersistence<T> where T : DomainEntity
    {
        protected IDatabaseWrapper Database { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CouchbaseLocal{T}"/> class.
        /// </summary>
        /// <exception cref="Exception">Repository Exception: Database name cannot be null or empty!</exception>
        public CouchbaseLocal(IDatabaseWrapper databaseWrapper)
        {
            Limit = 0;
            Database = databaseWrapper;
        }

        /// <summary>
        /// Called when [database change event].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DatabaseChangedEventArgs"/> instance containing the event data.</param>
        private void OnDatabaseChangeEvent(object sender, DatabaseChangedEventArgs e)
        {
            foreach (var documentId in e.DocumentIDs)
            {
                var document = Database?.GetDocument(documentId);
                string message = $"Document (id={documentId}) was ";


                if (document == null)
                {
                    message += "deleted";
                }
                else
                {
                    message += "added/updated";
                }

                Debug.WriteLine(message);
            }
        }

        protected string CreateKey(Guid id)
        {
            //Generates a key with the following format 'script::00000000-0000-0000-0000-000000000000'
            return $"{typeof(T).Name.ToLower()}::{id}";
        }

        public int Limit { get; set; }

        /// <summary>
        /// Queries on a specific field. Is only performant for fields that are indexed in the database.
        /// </summary>
        /// <param name="searchField">The search field.</param>
        /// <param name="value">The value.</param>
        /// <param name="caseSensitive">true, if case sensitive</param>
        /// <param name="waitForIndex"></param>
        /// <returns>C# object T</returns>
        public async Task<T> QueryOnFieldAsync(string searchField, string value, bool caseSensitive = true,
            bool waitForIndex = false)
        {
            var result = await QueryOnFieldAsync(searchField, value, 1, caseSensitive);
            return result?.FirstOrDefault();
        }

        /// <summary>
        /// Queries on a specific field. Is only performant for fields that are indexed in the database.
        /// </summary>
        /// <param name="searchField">The search field.</param>
        /// <param name="value">The value.</param>
        /// <param name="limit">The limit.</param>
        /// <param name="caseSensitive">true, if case sensitive</param>
        /// <param name="waitForIndex"></param>
        /// <returns>List of C# object T </returns>
        public async Task<List<T>> QueryOnFieldAsync(string searchField, string value, int limit,
            bool caseSensitive = true, bool waitForIndex = false)
        {
            ILimit query;
            if (limit == 0)
            {
                query = (ILimit)QueryBuilder.Select(SelectResult.All()).From(Database.GetDataSource())
                    .Where(Expression.Property("Type").EqualTo(Expression.String(typeof(T).Name))
                        .And((caseSensitive
                                ? Expression.Property(searchField)
                                : Function.Lower(Expression.Property(searchField)))
                            .EqualTo(
                                caseSensitive ? Expression.String(value) : Function.Lower(Expression.String(value)))));
            }
            else
            {
                query = QueryBuilder.Select(SelectResult.All()).From(Database.GetDataSource())
                    .Where(Expression.Property("Type").EqualTo(Expression.String(typeof(T).Name))
                        .And((caseSensitive
                                ? Expression.Property(searchField)
                                : Function.Lower(Expression.Property(searchField)))
                            .EqualTo((caseSensitive
                                ? Expression.String(value)
                                : Function.Lower(Expression.String(value))))))
                    .Limit(Expression.Int(limit));
            }

            var queryResults = await Task.Run(() => query.Execute());
            return SerializeQueryResults(queryResults);
        }

        /// <summary>
        /// Returns the query result as a list of objects.
        /// </summary>
        /// <param name="waitForIndex"></param>
        /// <param name="args">The arguments.</param>
        /// <returns>
        /// List of JSON strings
        /// </returns>
        public async Task<List<T>> QueryOnFieldsAsync(bool waitForIndex = false, params Tuple<string, object>[] args)
        {
            var expression = QueryBuilder.Select(SelectResult.All()).From(Database.GetDataSource())
                .Where(Expression.Property("Type").EqualTo(Expression.String(typeof(T).Name))
                    .WithArgs(args));

            if (Limit > 0)
            {
                expression.Limit(Expression.Int(Limit));
            }

            var queryResults = await Task.Run(() => expression.Execute());
            return SerializeQueryResults(queryResults);
        }

        /// <summary>
        /// Gets the object by Id.
        /// </summary>
        /// <param name="id">The id used to build the key for the document</param>
        /// <returns>JSON string</returns>
        public async Task<T> GetAsync(Guid id)
        {
            var key = CreateKey(id);
            return await GetAsync(key);
        }

        /// <summary>
        /// Gets the object by key {type::id}
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>JSON string</returns>
        public virtual async Task<T> GetAsync(string key)
        {
            var doc = await GetCouchbaseDocumentAsync(key);
            return doc?.ToObject<T>();
        }


        /// <summary>
        /// Creates a new key with the object's id and upserts the dictionary representation of that object.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="item"></param>
        public async Task UpsertAsync(Guid id, T item)
        {
            var key = CreateKey(id);
            await UpsertAsync(key, item);
        }

        /// <summary>
        /// Upserts the dictionary representation of the object.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="item"></param>
        public virtual async Task UpsertAsync(string key, T item)
        {
            item.UpdateDateUpdated();
            //Mutable document version of the item we're trying to save
            var mutableDoc = item.ToMutableDocument(key);

            //Dictionary is required to update an individual property (as opposed to overwriting the full document)
            var priorDocument = await GetCouchbaseDocumentAsync(key);
            if (priorDocument.IsNullOrEmpty())
            {
                //If document does not exist then create it
                await SaveAsync(mutableDoc, item);
            }
            else
            {
                //Differential update

                //Get a mutable document version of the currently persisted item and then compare document versions
                var documentToUpdate = priorDocument.ToMutable();
                var documentVersion = (int?)(long?)documentToUpdate.GetValue("DocumentVersion") ?? 0;
                var vesselDocumentVersion = item.DomainEntityVersion;

                //If we are trying to save a version lower than the current version of the document, merge the documents
                //in order to not overwrite any parameters not on our older version
                if (vesselDocumentVersion < documentVersion)
                {
                    var documentToSave = MergeVersionMismatchedDocuments(mutableDoc, documentToUpdate);
                    await SaveAsync(documentToSave, item);
                }
                //otherwise, just save the document, as the newer version won't delete fields or parameters unless we are
                //no longer supporting an older version
                else
                {
                    await SaveAsync(mutableDoc, item);
                }
            }
        }

        protected virtual async Task SaveAsync(MutableDocument mutableDoc, T item)
        {
            await Task.Run(() => Database.Save(mutableDoc));
        }

        /// <summary>
        /// This merges two documents with a differing schema. The purpose of this is so that we can have backwards compatibility
        /// and saving an older version of the document won't blow away new fields/parameters on the document.
        /// NOTE: This does not merge lists in case of a conflict.
        /// </summary>
        /// <param name="updateDocument"></param>
        /// <param name="originalDoc"></param>
        /// <returns></returns>
        private MutableDocument MergeVersionMismatchedDocuments(MutableDocument updateDocument,
            MutableDocument originalDoc)
        {
            var updateDocumentProperties = updateDocument.Keys;

            foreach (var prop in updateDocumentProperties)
            {
                //If GetArray returns !null, then we have a list of things. If GetDictionary returns !null, then we have a sub object
                var listOfThings = updateDocument.GetArray(prop);
                var subObject = updateDocument.GetDictionary(prop);
                if (listOfThings != null && listOfThings.Count > 0)
                {
                    //If there is anything in our list, merge our lists. This is necessary in case our list of things is a list of objects.
                    //Each of those objects might have a schema difference, so we need to merge them as well.
                    var mergedList = MergeEmbeddedListObjects(listOfThings, originalDoc.GetArray(prop));
                    originalDoc.SetValue(prop, mergedList);
                }
                else if (subObject != null)
                {
                    //If it is an object, merge them so we don't lose schema changes
                    var originalSubObject = originalDoc.GetDictionary(prop);
                    var mergedDict = MergeEmbeddedDictionaryObjects(subObject, originalSubObject);
                    originalDoc.SetDictionary(prop, mergedDict);
                }
                else
                {
                    //If its a simple property, take our value. Don't save our document version though, because it is still the higher version.
                    if (prop != "DocumentVersion")
                    {
                        var updateValue = updateDocument.GetValue(prop);
                        originalDoc.SetValue(prop, updateValue);
                    }
                }
            }

            return originalDoc;
        }

        /// <summary>
        /// Merge the schema of two lists and return the resulting list. Does not merge values of two lists,
        /// except to ensure that local additions/deletions are persisted. Will not handle database conflicts.
        /// </summary>
        /// <param name="newList"></param>
        /// <param name="originalList"></param>
        /// <returns></returns>
        private MutableArrayObject MergeEmbeddedListObjects(MutableArrayObject newList, MutableArrayObject originalList)
        {
            //if the original document doesn't have this list, just return the new list
            if (originalList == null)
            {
                return newList;
            }

            if (newList.Count > 0)
            {
                //If GetDictionary returns !null, then we have a list of objects and must merge schema accordingly.
                if (newList.GetDictionary(0) != null)
                {
                    //We have to do a 2-way check for objects here. First, we check to see if we deleted any objects currently on the original list
                    foreach (var originalKeyValuePair in originalList)
                    {
                        var originalKeyValueAsDict = (MutableDictionaryObject)originalKeyValuePair;
                        var id = (string)originalKeyValueAsDict.GetValue("EquivalenceValue") ??
                                 (string)originalKeyValueAsDict.GetValue("Id");

                        var shouldDelete = true;
                        foreach (var newKeyValuePair in newList)
                        {
                            var newKeyValueAsDict = (MutableDictionaryObject)newKeyValuePair;
                            var oid = (string)newKeyValueAsDict.GetValue("EquivalenceValue") ??
                                      (string)newKeyValueAsDict.GetValue("Id");
                            //If we find a match, we need to merge the two objects for schema changes
                            if (id == oid)
                            {
                                var mergedDict =
                                    MergeEmbeddedDictionaryObjects(newKeyValueAsDict, originalKeyValueAsDict);
                                //var index = originalList.
                                originalList.SetDictionary(originalList.IndexOf(originalKeyValuePair), mergedDict);
                                shouldDelete = false;
                            }
                        }

                        //if we loop through each object in the list and never get a match, then we need to remove the object from the original list.
                        if (shouldDelete)
                        {
                            originalList.RemoveAt(originalList.IndexOf(originalKeyValuePair));
                        }
                    }

                    //Second, we check to see if we need to add any objects to our list on our original list.
                    foreach (var newKeyValuePair in newList)
                    {
                        var newKeyValueDict = (MutableDictionaryObject)newKeyValuePair;
                        var id = (string)newKeyValueDict.GetValue("EquivalenceValue") ??
                                 (string)newKeyValueDict.GetValue("Id");
                        var shouldAdd = true;
                        foreach (var originalKeyValuePair in originalList)
                        {
                            var originalKeyValueDict = (MutableDictionaryObject)originalKeyValuePair;
                            var oid = (string)originalKeyValueDict.GetValue("EquivalenceValue") ??
                                      (string)originalKeyValueDict.GetValue("Id");
                            //If we find a match in the original document, then we don't need to add it
                            if (id == oid)
                            {
                                shouldAdd = false;
                            }
                        }

                        //If we don't ever find a match, then we need to add it to the original document.
                        //No need to merge, as it's a new object entirely.
                        if (shouldAdd)
                        {
                            originalList.AddDictionary(newKeyValueDict);
                        }
                    }
                }
                else
                {
                    //If it is not a list of objects, but a list of simple types, then just use our list we're trying to save
                    //This is where we can do a list merge - this is for a list of simple types
                    return newList;
                }
            }

            return originalList;
        }

        /// <summary>
        /// Merge two sub-objects together to keep schema changes across document versions
        /// </summary>
        /// <param name="newObject"></param>
        /// <param name="originalObject"></param>
        /// <returns></returns>
        private MutableDictionaryObject MergeEmbeddedDictionaryObjects(MutableDictionaryObject newObject,
            MutableDictionaryObject originalObject)
        {
            //If the object doesn't exist on the original document, just use the one we have
            if (originalObject == null)
            {
                return newObject;
            }

            //merge each parameter in the new object with it's pair in the original
            foreach (var parameter in newObject)
            {
                switch (parameter.Value)
                {
                    //If it's a list, merge the lists.
                    case MutableArrayObject updateList:
                        {
                            var newList =
                                MergeEmbeddedListObjects(updateList, originalObject.GetArray(parameter.Key));
                            originalObject.SetValue(parameter.Key, newList);
                            break;
                        }
                    //If it's a subobject, merge the objects
                    case MutableDictionaryObject updateObject:
                        {
                            var newDict = MergeEmbeddedDictionaryObjects(updateObject,
                                originalObject.GetDictionary(parameter.Key));
                            originalObject.SetValue(parameter.Key, newDict);
                            break;
                        }
                    //When we finally get down to simple types, just write our simple type
                    default:
                        originalObject.SetValue(parameter.Key, parameter.Value);
                        break;
                }
            }

            return originalObject;
        }

        /// <summary>
        /// Returns the query as string.
        /// </summary>
        /// <param name="limit">If 0, return all results without limit</param>
        /// <param name="waitForIndex">If true, tells couchbase to wait for all current indexing operations before running the query.</param>
        /// <returns>List of JSON strings</returns>
        public async Task<List<T>> GetAllOfTypeAsync(int limit = 0, bool waitForIndex = false)
        {
            ILimit query;
            if (limit != 0)
            {
                query = QueryBuilder.Select(SelectResult.All()).From(Database.GetDataSource())
                    .Where(Expression.Property("Type")
                        .EqualTo(Expression.String(typeof(T).Name)))
                    .Limit(Expression.Int(limit));
            }
            else
            {
                query = (ILimit)QueryBuilder.Select(SelectResult.All()).From(Database.GetDataSource())
                    .Where(Expression.Property("Type").EqualTo(Expression.String(typeof(T).Name)));
            }

            var queryResults = await Task.Run(() => query.Execute());
            return SerializeQueryResults(queryResults);
        }

        protected virtual List<T> SerializeQueryResults(IResultSet queryResults)
        {
            var results = queryResults.AllResults();
            var resultList = new List<T>();

            if (!results.Any())
            {
                return resultList;
            }

            resultList = results.ToObjects<T>().ToList();

            return resultList;
        }

        /// <summary>
        /// Returns the query for all documents with type that contains a sub string of the data persistence's type.
        /// </summary>
        /// <param name="searchField">The search field.</param>
        /// <param name="value">The value.</param>
        /// <returns>The string result of the query.</returns>
        public async Task<List<T>> ReturnTypeSubStringQueryAsStringAsync(string searchField, string value)
        {
            var query = (ILimit)QueryBuilder.Select(SelectResult.All()).From(Database.GetDataSource())
                .Where(Expression.Property("Type").Like(Expression.String($"%{typeof(T).Name}"))
                    .And(Expression.Property(searchField).Like(Expression.String(value))));

            var queryResults = await Task.Run(() => query.Execute());
            var results = queryResults.AllResults();
            var resultList = new List<T>();

            if (!results.Any())
            {
                return resultList;
            }

            resultList = results.ToObjects<T>().ToList();

            return resultList;
        }

        /// <summary>
        /// Deletes the document async.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public async Task DeleteAsync(Guid id)
        {
            var key = CreateKey(id);
            await DeleteAsync(key);
        }

        public virtual async Task DeleteAsync(string key)
        {
            var doc = await GetCouchbaseDocumentAsync(key);
            if (doc != null)
            {
                await Task.Run(() => Database.Delete(doc));
            }
        }

        protected async Task<Document> GetCouchbaseDocumentAsync(string key)
        {
            return await Task.Run(() => Database?.GetDocument(key));
        }

        public async Task<BatchStatus> BatchInsertAsync(List<T> list, bool forceFailure = false)
        {
            var mutableDocumentList = new List<MutableDocument>();
            foreach (var item in list)
            {
                var key = CreateKey(item.Id);
                mutableDocumentList.Add(item.ToMutableDocument(key));
            }
            return await Task.Run(() => Database.BatchInsertSynchronous(mutableDocumentList));
        }

        public async Task<BatchStatus> BatchDeleteAsync(List<Guid> guids, bool forceFailure = false)
        {
            var documentList = new List<Document>();
            foreach (var guid in guids)
            {
                var key = CreateKey(guid);
                var doc = await GetCouchbaseDocumentAsync(key);
                if (doc != null)
                {
                    documentList.Add(doc);
                }
            }

            return await Task.Run(() => Database.BatchDeleteSynchronous(documentList));
        }

        public async Task PurgeAllOfTypeForProjectId(Guid projectId)
        {
            var documents = await QueryOnFieldAsync("ProjectId",
                projectId.ToString(), 0);
            foreach (var key in documents.Select(document => CreateKey(document.Id)))
            {
                var doc = await GetCouchbaseDocumentAsync(key);
                Database.Purge(doc);
            }
        }
        
        public async Task PurgeAllOfTypeForId(Guid id)
        {
            var documents = await QueryOnFieldAsync("Id",
                id.ToString(), 0);
            foreach (var key in documents.Select(document => CreateKey(document.Id)))
            {
                var doc = await GetCouchbaseDocumentAsync(key);
                Database.Purge(doc);
            }
        }
        
        public async Task ResetCreatedFromAudioIds(Guid projectId, List<Guid> deletedStandardQuestions)
        {
            if (!deletedStandardQuestions.Any()) { return; }

            var updateCriteria = Expression
                .Property(nameof(NotableAudio.ProjectId))
                .EqualTo(Expression.String(projectId.ToString()))
                .And(Expression
                    .Property(nameof(NotableAudio.CreatedFromAudioId))
                    .In(deletedStandardQuestions.Select(audioId => Expression.String(audioId.ToString())).ToArray())
                );

            await Task.Run(() => Database.BatchUpdateSynchronous(updateCriteria, mutableDocument =>
            {
                mutableDocument.SetValue(nameof(NotableAudio.CreatedFromAudioId), default(Guid).ToString());
                mutableDocument.SetValue(nameof(NotableAudio.DateUpdated), DateTimeOffset.UtcNow);
            }));
        }

        public void Dispose()
        {
            //Database?.Dispose();
        }
    }

    internal class CustomPropertyNameConverter : IPropertyNameConverter
    {
        public string Convert(string propertyName)
        {
            return propertyName;
        }
    }
}