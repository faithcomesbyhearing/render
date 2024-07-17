using System.Reflection;
using Couchbase.Lite;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ReactiveUI;

namespace Render.TempFromVessel.Document_Extensions
{
    public static class DocumentExtensions
    {
        //Takes a document from couchbase local and turns it into an object
        public static T ToObject<T>(this Document document)
        {
            T obj = default(T);

            try
            {
                if (document != null)
                {
                    if (document.ToDictionary()?.Count > 0)
                    {
                        //This makes sure we get polymorphic lists
                        var settings = new JsonSerializerSettings
                        {
                            ContractResolver = ShouldSerializeContractResolver.Instance
                        };

                        var dictionary = document.ToMutable()?.ToDictionary();

                        if (dictionary != null)
                        {
                            var json = JsonConvert.SerializeObject(dictionary);

                            if (!string.IsNullOrEmpty(json))
                            {
                                obj = JsonConvert.DeserializeObject<T>(json, settings);
                            }
                        }
                    }
                    else
                    {
                        obj = Activator.CreateInstance<T>();
                    }
                }
            }
            catch
            {
                // ignored
            }

            return obj;
        }
        
        public static T ToAudioObject<T>(this Document document)
        {
            T obj = default(T);

            try
            {
                if (document != null)
                {
                    if (document.ToDictionary()?.Count > 0)
                    {
                        //This makes sure we get polymorphic lists
                        var settings = new JsonSerializerSettings
                        {
                            ContractResolver = ShouldSerializeContractResolver.Instance
                        };

                        var dictionary = document.ToMutable()?.ToDictionary();

                        if (dictionary != null)
                        {
                            //Remove both possible blob keys for the sake of serializing the object
                            dictionary.Remove("blob_/audio");
                            dictionary.Remove("_attachments");
                            var json = JsonConvert.SerializeObject(dictionary);

                            if (!string.IsNullOrEmpty(json))
                            {
                                obj = JsonConvert.DeserializeObject<T>(json, settings);
                            }
                        }
                    }
                    else
                    {
                        obj = Activator.CreateInstance<T>();
                    }
                }
            }
            catch
            {
                // ignored
            }

            return obj;
        }
    }

    public class ShouldSerializeContractResolver : DefaultContractResolver
    {
        public static ShouldSerializeContractResolver Instance { get; } = new ShouldSerializeContractResolver();
        
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);        
            if (typeof(ReactiveObject).IsAssignableFrom(member.DeclaringType) &&
                member.Name 
                    is nameof(ReactiveObject.Changed) 
                    or nameof(ReactiveObject.Changing)
                    or nameof(ReactiveObject.ThrownExceptions))
            {
                property.Ignored = true;
            }
            return property;
        }
    }
}
