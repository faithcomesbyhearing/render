namespace Render.Resources
{
    public static class ResourceExtensions
    {
        public static object GetResourceValue(string keyName)
        {
            return GetResourceValue<object>(Application.Current?.Resources, keyName);
        }

        public static T GetResourceValue<T>(string keyName)
        {
            return GetResourceValue<T>(Application.Current?.Resources, keyName);
        }

        public static T GetResourceValue<T>(ResourceDictionary resources, string keyName)
        {
            object retVal = null;
            // Search all dictionaries
            if (resources?.TryGetValue(keyName, out retVal) == true)
            {
                return (T)retVal;
            }
            return (T)retVal;
        }

        public static Color GetColor(string colorReferenceKey)
        {
            return ((Styles.ColorReference)GetResourceValue(colorReferenceKey))?.Color;
        }
    }
}