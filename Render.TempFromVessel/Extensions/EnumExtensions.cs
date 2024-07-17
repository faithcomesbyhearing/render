using System.ComponentModel;
using System.Reflection;

namespace Render.TempFromVessel.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            DescriptionAttribute descriptionAttribute;

            if (name != null && 
               (descriptionAttribute = type.GetField(name)?.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute) != null)
            {
                return descriptionAttribute.Description;
            }

            return null;
        }
    }
}