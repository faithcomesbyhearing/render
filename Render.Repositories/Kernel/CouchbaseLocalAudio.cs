using Couchbase.Lite;
using Couchbase.Lite.Query;
using Render.TempFromVessel.Document_Extensions;

namespace Render.Repositories.Kernel
{
    public class CouchbaseLocalAudio<T> : CouchbaseLocal<T> where T : Models.Audio.Audio
    {
        public CouchbaseLocalAudio(IDatabaseWrapper databaseWrapper)
            : base(databaseWrapper) { }

        protected override async Task SaveAsync(MutableDocument mutableDoc, T item)
        {
            var audioBlob = new Blob("audio/opus", item.Data);
            mutableDoc.SetBlob("blob_/audio", audioBlob);

            await Task.Run(() => Database.Save(mutableDoc));
        }

        public override async Task<T> GetAsync(string key)
        {
            var doc = await GetCouchbaseDocumentAsync(key);
            if (doc == null) return null;

            var audioObject = doc.ToAudioObject<T>();
            var blob = doc.GetBlob("blob_/audio");
            if (blob != null)
            {
                audioObject.SetAudio(blob.Content, blob.Length, blob.Digest); 
            }

            return audioObject;
        }

        protected override List<T> SerializeQueryResults(IResultSet queryResults)
        {
            var results = queryResults.AllResults();
            var resultList = new List<T>();

            if (!results.Any())
            {
                return resultList;
            }

            resultList = results.ToAudioObjects<T>().ToList();

            for (var i = 0; i < results.Count; i++)
            {
                var result = results[i];
                var dict = result.ToDictionary();
                var innerDictionary = (Dictionary<string, object>)dict[result.Keys.FirstOrDefault()];
                object blob;
                //Check if there's a blob stored in the way Render adds it
                if (innerDictionary.ContainsKey("blob_/audio"))
                {
                    blob = innerDictionary["blob_/audio"];
                }
                //If not, check if there's a blob in the way Launchpad adds it
                else if (innerDictionary.ContainsKey("_attachments"))
                {
                    //The Launchpad blobs are stored another layer down in the dictionary structure
                    innerDictionary = (Dictionary<string, object>)innerDictionary["_attachments"];
                    blob = innerDictionary["blob_/blob_~1audio"];
                }
                //If not, loop to the next document
                else
                {
                    continue;
                }
                
                if (blob != null)
                {
                    var b = (Blob)blob;
                    resultList[i].SetAudio(b.Content, b.Length, b.Digest);
                }
            }

            return resultList;
        }
        
        public override async Task DeleteAsync(string key)
        {
            await base.DeleteAsync(key);
        }
    }
}