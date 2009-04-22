using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnHood.Engine.UE3
{
    class UE3PropertyListReader: PropertyListReader
    {
        public UnPropertyList ReadPropertyList(UnPackage package, BinaryReader reader, UnExport export, UnClass owner)
        {
            return new UE3PropertyListInstanceReader(package, reader, export, owner).Read();
        }
    }

    internal class UE3PropertyListInstanceReader
    {
        private readonly UnPackage _package;
        private readonly BinaryReader _reader;
        private readonly UnExport _export;
        private readonly UnContainer _owner;

        public UE3PropertyListInstanceReader(UnPackage package, BinaryReader reader, UnExport export, UnContainer owner)
        {
            _package = package;
            _reader = reader;
            _export = export;
            _owner = owner;
        }

        public UnPropertyList Read()
        {
            var result = new UnPropertyList(_export);
            _reader.ReadInt32();
            while (true)
            {
                if (!ReadProperty(result))
                    break;
            }
            return result;
        }

        private bool ReadProperty(UnPropertyList result)
        {
            var nameIndex = _reader.ReadInt64();
            var name = _package.Names[(int)nameIndex].Name;
            if (name == "None")
                return false;
            var typeIndex = _reader.ReadInt64();
            var type = _package.Names[(int)typeIndex].Name;
            var valueSize = _reader.ReadInt32();
            var i = _reader.ReadInt32();
            object value;
            if (type == "StructProperty")
            {
                _reader.ReadInt64();  // struct name index
                var pos = _reader.BaseStream.Position;
                value = ReadStructProperty(_owner, name);
                _reader.BaseStream.Position = pos + valueSize;
                if (value == null) return true;   // skip native structs
            }
            else
            {
                var pos = _reader.BaseStream.Position;
                value = ReadSingleValue(_owner, name, type);
                if (value == null)
                {
                    _reader.BaseStream.Position = pos;
                    value = _reader.ReadBytes(valueSize);
                }
            }
            result.AddProperty(name, type, value);
            return true;
        }

        private object ReadSingleValue(UnContainer owner, string name, string type)
        {
            if (type == "FloatProperty")
                return _reader.ReadSingle();
            if (type == "BoolProperty")
                return _reader.ReadInt32() != 0;
            if (type == "IntProperty")
                return _reader.ReadInt32();
            if (type == "StrProperty")
            {
                var valueLength = _reader.ReadInt32();
                object value;
                if (valueLength < 0)
                {
                    var s = _reader.ReadBytes(-2 * valueLength);
                    value = new string(Encoding.Unicode.GetChars(s));
                }
                else
                {
                    value = new string(_reader.ReadChars(valueLength));
                }
                return ((string)value).Trim('\0');
            }
            if (type == "NameProperty")
            {
                var valueNameIndex = _reader.ReadInt64();
                return _package.Names[(int)valueNameIndex].Name;
            }
            if (type == "ByteProperty")
            {
                var export = owner.FindMemberExport(name);
                var declaration = (UnTypedClassProperty) export.ReadInstance();
                if (declaration.Type == null)
                    return _reader.ReadByte();

                var valueNameIndex = _reader.ReadInt64();
                if (valueNameIndex < 0 || valueNameIndex >= _package.Names.Count)
                    return null;
                return _package.Names[(int)valueNameIndex].Name;
            }
            if (type == "ObjectProperty")
            {
                var export = owner.FindMemberExport(name);
                var index = _reader.ReadInt32();
                if (index == 0) return "None";
                var item = _package.ResolveClassItem(index);
                if (export.ClassName == "ClassProperty")
                {
                    return "class'" + item.ObjectName + "'";
                }
                return item.ClassName + "'" + item.ObjectName + "'";
            }
            if (type == "ClassProperty")
            {
                var index = _reader.ReadInt32();
                if (index == 0) return "None";
                var item = _package.ResolveClassItem(index);
                return "class'" + item.ObjectName + "'";
            }
            if (type == "ArrayProperty")
            {
                var export = owner.FindMemberExport(name);
                var prop = (UnArrayClassProperty) export.ReadInstance();
                var propType = prop.Type.Resolve();

                var count = _reader.ReadInt32();
                var result = new UnPropertyArray(propType.ClassName);
                for(int i=0; i<count; i++)
                {
                    var value = ReadSingleValue(owner, name, propType.ClassName);
                    if (value == null) return null;
                    result.Add(value);
                }
                return result;
            }
            return null;
        }

        private UnPropertyList ReadStructProperty(UnContainer owner, string name)
        {
            var export = owner.FindMemberExport(name);
            if (export == null) return null;
            var declaration = (UnTypedClassProperty) export.ReadInstance();
            var typeDeclaration = declaration.Type;
            UnExport scriptStruct = typeDeclaration.Resolve();
            var structInstance = (UnScriptStruct)scriptStruct.ReadInstance();
            if (structInstance.Native)
                return null;

            var result = new UnPropertyList(null);
            ReadStructValues(scriptStruct, result);
            return result;
        }

        private bool ReadStructValues(UnExport scriptStruct, UnPropertyList result)
        {
            bool haveUnknownValues = false;
            var structInstance = (UnScriptStruct) scriptStruct.ReadInstance();
            if (structInstance.Super != null)
            {
                haveUnknownValues = ReadStructValues(structInstance.Super.Resolve(), result);
            }
            foreach (UnExport e in scriptStruct.Children.Reverse())
            {
                object value;
                if (haveUnknownValues)
                    value = null;
                else if (e.ClassName == "StrProperty")
                {
                    ReadProperty(result);
                    continue;
                }
                else
                {
                    value = ReadSingleValue(structInstance, e.ObjectName, e.ClassName);
                    if (value == null) haveUnknownValues = true;
                }
                result.AddProperty(e.ObjectName, e.ClassName, value);
            }
            return haveUnknownValues;
        }
    }
}
