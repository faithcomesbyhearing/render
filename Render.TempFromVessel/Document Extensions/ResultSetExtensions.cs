using Couchbase.Lite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Render.TempFromVessel.Document_Extensions
{
    public static class ResultSetExtensions
    {
        public static T ToObject<T>(this Couchbase.Lite.Query.Result result)
        {
            T obj = default;

            if (result != null)
            {
                JObject rootJObj = new JObject();

                foreach (var key in result.Keys)
                {
                    var value = result[key]?.Value;
                    
                    if (value != null)
                    {
                        JObject jObj = null;

                        if (value.GetType() == typeof(DictionaryObject))
                        {
                            var json = result.ToJSON();
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

        public static IEnumerable<T> ToObjects<T>(this List<Couchbase.Lite.Query.Result> results)
        {
            List<T> objects = default;

            if (results?.Count > 0)
            {
                var settings = new JsonSerializerSettings()
                {
                    ContractResolver = ShouldSerializeContractResolver.Instance
                };

                objects = new List<T>();

                foreach (var result in results)
                {
                    var obj = ToObject<T>(result);

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