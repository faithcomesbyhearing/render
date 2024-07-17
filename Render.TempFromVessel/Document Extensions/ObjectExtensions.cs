using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using Couchbase.Lite;
using Newtonsoft.Json;
using Render.TempFromVessel.Document_Extensions.Attributes;
using Render.TempFromVessel.Document_Extensions.Converters;
using Render.TempFromVessel.User;

namespace Render.TempFromVessel.Document_Extensions
{
    ///<summary>
    /// This class is to take an object and convert it to a mutable document for saving to Couchbase Local
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// This is the base call made to turn a C# object into a document for saving
        /// </summary>
        public static MutableDocument ToMutableDocument<T>(
            this T obj, 
            string id, 
            IPropertyNameConverter propertyNameConverter = null)
        {
            var document = new MutableDocument(id);

            var dictionary = GetDictionary(obj, propertyNameConverter);

            if (dictionary != null)
            {
                document.SetData(dictionary);
            }

            return document;
        }

        /// <summary>
        /// This takes an object (including sub-objects like lists of objects or dictionaries,)
        /// and turns it into the appropriate dictionary, which is used as the data of our CouchbaseLite document
        /// </summary>
        static Dictionary<string, object> GetDictionary(object obj, IPropertyNameConverter propertyNameConverter = null)
        {
            var dictionary = new Dictionary<string, object>();
            //Get all properties of the object via reflection so we know what we need to put in our dictionary (fields will not be persisted)
            var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(pi => !Attribute.IsDefined(pi, typeof(JsonIgnoreAttribute)))?.ToList();

            //For each property on the object, get the appropriate property name and add the property value to our dictionary appropriately
            foreach (PropertyInfo propertyInfo in properties)
            {
                string propertyName;
            
                var propertyValue = propertyInfo.GetValue(obj);
                var propertyType = propertyInfo.PropertyType;

                if (propertyType.IsEnum)
                {
                    var attribute = propertyInfo.PropertyType.GetMember(propertyValue.ToString()).FirstOrDefault()?.GetCustomAttribute<EnumMemberAttribute>();
                    if (attribute != null)
                    {
                        propertyValue = attribute.Value;
                    }
                }

                if (propertyValue != null)
                {
                    if (propertyInfo.CustomAttributes?.Count() > 0 &&
                        propertyInfo.GetCustomAttribute(typeof(MappingPropertyName)) is MappingPropertyName mappingProperty)
                    {
                        propertyName = mappingProperty.Name;
                    }
                    else if (propertyInfo.CustomAttributes?.Count() > 0 &&
                             propertyInfo.GetCustomAttribute(typeof(JsonPropertyAttribute)) is JsonPropertyAttribute jsonProperty)
                    {
                        propertyName = jsonProperty.PropertyName;
                    }
                    else if (propertyNameConverter != null)
                    {
                        propertyName = propertyNameConverter.Convert(propertyInfo.Name);
                    }
                    else
                    {
                        propertyName = Settings.PropertyNameConverter.Convert(propertyInfo.Name);
                    }

                    //Go do property gymnastics to determine what our actual dictionary value needs to be
                    AddDictionaryValue(ref dictionary, propertyName, propertyValue, propertyInfo.PropertyType, propertyNameConverter);
                }
            
            }

            return dictionary;
        }

        /// <summary>
        /// Take in a property name and value, and add the appropriate key/value pair to our CouchbaseLite document dictionary
        /// Each property type (beyond simple types) must be massaged to be serialized/deserialized appropriately
        /// </summary>
        static void AddDictionaryValue(
            ref Dictionary<string, object> dictionary, 
            string propertyName,
            object propertyValue, 
            Type propertyType,
            IPropertyNameConverter propertyNameConverter = null)
        {
            //enforce DateTimeOffset rather than DateTime or nullable DateTime
            if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
            {
                var dateTimeVal = ((DateTime)propertyValue);
                if (dateTimeVal != default(DateTime))
                {
                    dictionary[propertyName] = new DateTimeOffset((DateTime)propertyValue);
                }
            }
            //If the property is a list of Guids, convert each Guid to a string to remove curly braces from the Guid in the dictionary
            //(curly braces break serialization)
            else if (typeof(IEnumerable).IsAssignableFrom(propertyType) && !propertyType.IsArray && propertyValue is IList && propertyValue.GetType().GetTypeInfo().GenericTypeArguments[0] == typeof(Guid))
            {
                var newList = new List<string>();
                foreach (var property in (List<Guid>)propertyValue)
                {
                    newList.Add(property.ToString());
                }
                dictionary[propertyName] = newList;
            }
            //If our property is another object or list of objects, create a dictionary of just that object or that list of objects
            //This will nest our dictionaries to replicate what a CouchbaseLite document looks like
            else if (
                !propertyType.IsSimple() && 
                !propertyType.IsEnum && 
                (propertyType.IsClass || propertyType.IsInterface) 
                && propertyValue != null)
            {
                if (typeof(IEnumerable).IsAssignableFrom(propertyType))
                {
                    if (
                        propertyType.IsArray && 
                        propertyType.GetElementType().IsSimple() || 
                        (!propertyType.IsArray && 
                            propertyValue is IList && 
                            propertyValue.GetType().GetTypeInfo().GenericTypeArguments[0].IsSimple()))
                    {
                        dictionary[propertyName] = propertyValue;
                    }
                    //This is where our sub-objects or list of sub-objects are mapped
                    else
                    {
                        
                        var items = propertyValue as IEnumerable;
                        var dictionaries = new List<Dictionary<string, object>>();

                        foreach (var item in items)
                        {
                            var internalDictionary = GetDictionary(item, propertyNameConverter);
                            //This check is a band-aid for PBI 6261, but should be evaluated further
                            if (item is not VesselClaim)
                            {
                                //This is forcing a hidden type field on the document that allows for polymorphism when retrieving the document and deserializing
                                internalDictionary.Add("$type", $"{item.GetType()}, Render.Models");
                            }
                            dictionaries.Add(internalDictionary);
                        }
                        if (dictionaries.Count == 0)
                        {
                            //NO TOUCHY! If there is nothing in our dictionaries at this point, don't include it in our JSON document. It will break, and you will be sad.
                        }
                        else
                        {
                            dictionary[propertyName] = dictionaries.ToArray();
                        }

                    }
                }
                else
                {
                    dictionary[propertyName] = GetDictionary(propertyValue, propertyNameConverter);
                }
            }
            //If the property is a Guid, convert it to a string to remove the curly braces from the Guid in the dictionary.
            //curly braces break serialization
            else if (propertyType.IsEnum || propertyType == typeof(Guid))
            {
                dictionary[propertyName] = propertyValue.ToString();
            }
            else
            {
                dictionary[propertyName] = propertyValue;
            }
        }

        static bool IsSimple(this Type type) => (type.IsValueType || type == typeof(string));
    }
}