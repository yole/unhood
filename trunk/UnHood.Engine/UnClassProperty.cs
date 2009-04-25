using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnHood.Engine
{
    class UnClassProperty: Decompilable
    {
        protected readonly UnExport _export;
        private readonly int _arraySize;
        private readonly FlagValues _flags;
        private readonly UnName _category;

        public UnClassProperty(UnExport export, int arraySize, FlagValues flags, int category, short repOffset)
        {
            _export = export;
            _arraySize = arraySize;
            _flags = flags;
            var categoryName = export.Package.Names[category];
            if (categoryName.Name != "None")
                _category = categoryName;
            RepOffset = repOffset;
        }

        internal string Name { get { return _export.ObjectName; } }
        internal FlagValues Flags { get { return _flags; } }
        internal bool Parm { get { return _flags.HasFlag("Parm"); } }
        internal bool ReturnParm { get { return _flags.HasFlag("ReturnParm"); } }
        internal bool OptionalParm { get { return _flags.HasFlag("Optional"); } }

        public virtual void Decompile(TextBuilder result)
        {
            result.Indent();
            result.Append("var");
            if (_flags.HasFlag("Edit"))
            {
                result.Append("(");
                if (_category != null && _category.Name != _export.Parent.ObjectName)
                    result.Append(_category.Name);
                result.Append(")");
            }
            _flags.Except("Edit").Each(f => result.Append(" " + f.ToLower()));
            result.Append(" ").Append(GetPropertyType()).Append(" ");
            result.Append(_export.ObjectName);
            if (_arraySize != 1)
                result.Append("[").Append(_arraySize).Append("]");
            result.Append(";\n\n");
        }

        protected internal virtual string GetPropertyType()
        {
            if (_export.ClassName == "BoolProperty")
                return "bool";
            if (_export.ClassName == "IntProperty")
                return "int";
            if (_export.ClassName == "FloatProperty")
                return "float";
            if (_export.ClassName == "StrProperty")
                return "String";
            if (_export.ClassName == "NameProperty")
                return "Name";
            return "???";
        }

        public short RepOffset { get; private set; }
    }

    class UnTypedClassProperty: UnClassProperty
    {
        public UnTypedClassProperty(UnExport export, int arraySize, FlagValues flags, int category, UnPackageItem type, short repOffset) 
            : base(export, arraySize, flags, category, repOffset)
        {
            Type = type;
        }

        protected internal UnPackageItem Type { get; private set; }

        protected internal override string GetPropertyType()
        {
            if (Type == null && _export.ClassName == "ByteProperty") return "byte";
            if (Type == null) return "???";
            return Type.ObjectName;
        }
    }

    class UnArrayClassProperty: UnTypedClassProperty
    {
        public UnArrayClassProperty(UnExport export, int arraySize, FlagValues flags, int category, UnPackageItem type, short repOffset) 
            : base(export, arraySize, flags, category, type, repOffset)
        {
        }

        protected internal override string GetPropertyType()
        {
            if (Type is UnExport)
            {
                var instance = ((UnExport) Type).ReadInstance();
                if (instance is UnClassProperty)
                {
                    var type = ((UnClassProperty) instance).GetPropertyType();
                    if (type.EndsWith(">"))
                        type += " ";
                    return "array<" + type + ">";
                }
            }
            return "array<???>";
        }
    }

    class UnClassClassProperty: UnClassProperty
    {
        public UnClassClassProperty(UnExport export, int arraySize, FlagValues flags, int category, UnPackageItem type1, UnPackageItem type2, short repOffset) 
            : base(export, arraySize, flags, category, repOffset)
        {
            Type1 = type1;
            Type2 = type2;
        }

        public UnPackageItem Type1 { get; private set; }
        public UnPackageItem Type2 { get; private set; }

        protected internal override string GetPropertyType()
        {
            return "class<" + Type2.ObjectName + ">";
        }
    }

    class UnDelegateClassProperty: UnClassProperty
    {
        private readonly UnPackageItem _type;

        public UnDelegateClassProperty(UnExport export, int arraySize, FlagValues flags, int category, UnPackageItem type, short repOffset)
            : base(export, arraySize, flags, category, repOffset)
        {
            _type = type;
        }

        public UnPackageItem Type
        {
            get { return _type; }
        }

        protected internal override string GetPropertyType()
        {
            return "delegate<" + _type.ObjectName + ">";
        }

        public override void Decompile(TextBuilder result)
        {
            if (_export.ObjectName.StartsWith("__") && _export.ObjectName.EndsWith("__Delegate"))
            {
                // skip synthetic property
                return;
            }
            base.Decompile(result);
        }
    }
}
