using Couchbase.Lite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Render.TempFromVessel.Document_Extensions
{
    public static class AudioResultSetExtensions
    {
        public static T ToAudioObject<T>(this Couchbase.Lite.Query.Result result)
        {
            T obj = default;

            if (result != null)
            {
                var settings = new JsonSerializerSettings()
                {
                    ContractResolver = ShouldSerializeContractResolver.Instance
                };

                JObject rootJObj = new JObject();

                foreach (var key in result.Keys)
                {
                    var value = result[key]?.Value;
                    
                    if (value != null)
                    {
                        JObject jObj = null;

                        if (value.GetType() == typeof(DictionaryObject))
                        {
                            var dict = result.ToDictionary();
                            //Remove both possible blob keys for the sake of serializing the object
                            ((Dictionary<string, object>)dict[key]).Remove("blob_/audio");
                            ((Dictionary<string, object>)dict[key]).Remove("_attachments");
                            var json = JsonConvert.SerializeObject(dict);
                            var index = json.IndexOf(":");
                            json = json.Substring(index + 1, json.Length - 2 - index);

                            if (key == "renderaudio")
                            {
                                obj = JsonConvert.DeserializeObject<T>(json);
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(json))
                                {
                                    jObj = JObject.Parse(json);
                                }    
                            }
                        }
                        else
                        {
                            jObj = new JObject
                            {
                                new JProperty(key, value)
                            };
                        }
                        
                        if (jObj != null)
                        {
                            rootJObj.Merge(jObj, new JsonMergeSettings
                            {
                                // Union array values together to avoid duplicates (e.g. "id")
                                MergeArrayHandling = MergeArrayHandling.Union
                            });
                        }

                        if (rootJObj.HasValues)
                        {
                            obj = rootJObj.ToObject<T>();
                        } 
                        
                    }
                }
            }

            return obj;
        }

        public static IEnumerable<T> ToAudioObjects<T>(this List<Couchbase.Lite.Query.Result> results)
        {
            List<T> objects = default;

            if (results?.Count > 0)
            {
                objects = new List<T>();

                foreach (var result in results)
                {
                    var obj = ToAudioObject<T>(result);

                    if (obj != null)
                    {
                        objects.Add(obj);
                    }
                }
            }

            return objects;
        }   
    }
}