using System;

namespace Render.TempFromVessel.Document_Extensions.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MappingPropertyName : Attribute
    {
        public string Name { get; private set; }

        public MappingPropertyName(string name)
        {
            Name = name;
        }
    }
}