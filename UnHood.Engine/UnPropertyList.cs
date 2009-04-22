using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace UnHood.Engine
{
    class UnProperty
    {
        public UnProperty(string name, string type, object value)
        {
            Name = name;
            Type = type;
            Value = value;
        }

        public string Name { get; private set; }
        public string Type { get; private set; }
        public object Value { get; private set; }
    }

    class UnPropertyList
    {
        public UnPropertyList(UnExport export)
        {
            Export = export;
        }

        public UnExport Export { get; private set; }
        private readonly List<UnProperty> _properties = new List<UnProperty>();

        public void AddProperty(string name, string type, object value)
        {
            _properties.Add(new UnProperty(name, type, value));
        }

        public ReadOnlyCollection<UnProperty> Properties
        {
            get { return _properties.AsReadOnly(); }
        }
    }

    class UnPropertyArray: List<object>
    {
        public UnPropertyArray(string elementType)
        {
            ElementType = elementType;
        }

        public string ElementType { get; private set; }
    }
}
