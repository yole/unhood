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
        private readonly int _flags;
        private readonly UnName _category;

        private const int CPF_EDIT = 0x1;
        private const int CPF_CONST = 0x2;
        private const int CPF_OPTIONALPARM = 0x10;
        internal const int CPF_NET = 0x20;
        private const int CPF_PARM = 0x80;
        private const int CPF_OUTPARM = 0x100;
        private const int CPF_RETURNPARM  = 0x400;
        private const int CPF_COERCEPARM  = 0x800;
        private const int CPF_NATIVE  = 0x1000;
        private const int CPF_TRANSIENT  = 0x2000;
        private const int CPF_CONFIG  = 0x4000;
        private const int CPF_LOCALIZED  = 0x8000;

        public UnClassProperty(UnExport export, int arraySize, int flags, int category, short repOffset)
        {
            _export = export;
            _arraySize = arraySize;
            _flags = flags;
            var categoryName = export.Package.Names[category];
            if (categoryName.Name != "None")
                _category = categoryName;
            RepOffset = repOffset;
        }

        internal bool Edit { get { return (_flags & CPF_EDIT) != 0; } }
        internal bool Const { get { return (_flags & CPF_CONST) != 0; } }
        internal bool OutParm { get { return (_flags & CPF_OUTPARM) != 0; } }
        internal bool OptionalParm { get { return (_flags & CPF_OPTIONALPARM) != 0; } }
        internal bool Parm { get { return (_flags & CPF_PARM) != 0; } }
        internal bool ReturnParm { get { return (_flags & CPF_RETURNPARM) != 0; } }
        internal bool CoerceParm { get { return (_flags & CPF_COERCEPARM) != 0; } }
        internal bool Native { get { return (_flags & CPF_NATIVE) != 0; } }
        internal bool Localized { get { return (_flags & CPF_LOCALIZED) != 0; } }

        internal string Name
        {
            get { return _export.ObjectName; }
        }

        public virtual void Decompile(TextBuilder result)
        {
            result.Indent();
            result.Append("var");
            if (Edit)
            {
                result.Append("(");
                if (_category != null)
                    result.Append(_category.Name);
                result.Append(")");
            }
            if (Const) result.Append(" const");
            if (Native) result.Append(" native");
            if (Localized) result.Append(" localized");
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

        public int GetUnknownParamFlags()
        {
            return _flags & ~(CPF_OPTIONALPARM | CPF_PARM | CPF_RETURNPARM | CPF_COERCEPARM | CPF_CONST | CPF_OUTPARM);
        }

        public short RepOffset { get; private set; }
    }

    class UnTypedClassProperty: UnClassProperty
    {
        public UnTypedClassProperty(UnExport export, int arraySize, int flags, int category, UnPackageItem type, short repOffset) 
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
        public UnArrayClassProperty(UnExport export, int arraySize, int flags, int category, UnPackageItem type, short repOffset) 
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
        public UnClassClassProperty(UnExport export, int arraySize, int flags, int category, UnPackageItem type1, UnPackageItem type2, short repOffset) 
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

        public UnDelegateClassProperty(UnExport export, int arraySize, int flags, int category, UnPackageItem type, short repOffset)
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
