using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace UnHood.Engine.UE3
{
    class UE3ClassPropertyReader: InstanceReader
    {
        protected static FlagSet _flagSet = new FlagSet(
            "Edit", "Const", null, null,
            "Optional", "Net", null, "Parm",
            "Out", null, "ReturnParm", "Coerce",
            "Native", "Transient", "Config", "Localized");

        public object ReadInstance(UnPackage package, BinaryReader reader, UnExport export)
        {
            // 44 bytes
            reader.ReadBytes(16);
            int next = reader.ReadInt32();
            int arraySize = reader.ReadInt32();
            int flags = reader.ReadInt32();
            reader.ReadInt32();
            int category = (int) reader.ReadInt64();
            reader.ReadInt32();
            var repOffset = ((flags & _flagSet.GetMask("Net")) != 0) ? reader.ReadInt16() : (short)-1;
            return CreateProperty(export, arraySize, new FlagValues(flags, _flagSet), category, reader, repOffset);
        }

        protected virtual UnClassProperty CreateProperty(UnExport export, int arraySize, FlagValues flags, int category, BinaryReader reader, short repOffset)
        {
            return new UnClassProperty(export, arraySize, flags, category, repOffset);
        }
    }

    class UE3StructPropertyReader: UE3ClassPropertyReader
    {
        protected override UnClassProperty CreateProperty(UnExport export, int arraySize, FlagValues flags, int category, BinaryReader reader, short repOffset)
        {
            var typeItem = export.Package.ResolveClassItem(reader.ReadInt32());
            return new UnTypedClassProperty(export, arraySize, flags, category, typeItem, repOffset);
        }
    }

    class UE3TypedClassPropertyReader: UE3ClassPropertyReader
    {
        protected override UnClassProperty CreateProperty(UnExport export, int arraySize, FlagValues flags, int category, BinaryReader reader, short repOffset)
        {
            var typeItem = export.Package.ResolveClassItem(reader.ReadInt32());
            return new UnTypedClassProperty(export, arraySize, flags, category, typeItem, repOffset);
        }
    }

    class UE3ArrayClassPropertyReader: UE3ClassPropertyReader
    {
        protected override UnClassProperty CreateProperty(UnExport export, int arraySize, FlagValues flags, int category, BinaryReader reader, short repOffset)
        {
            var typeItem = export.Package.ResolveClassItem(reader.ReadInt32());
            return new UnArrayClassProperty(export, arraySize, flags, category, typeItem, repOffset);
        }
    }

    class UE3ClassClassPropertyReader : UE3ClassPropertyReader
    {
        protected override UnClassProperty CreateProperty(UnExport export, int arraySize, FlagValues flags, int category, BinaryReader reader, short repOffset)
        {
            int typeId1 = reader.ReadInt32();
            int typeId2 = reader.ReadInt32();
            var typeItem1 = export.Package.ResolveClassItem(typeId1);
            var typeItem2 = export.Package.ResolveClassItem(typeId2);
            return new UnClassClassProperty(export, arraySize, flags, category, typeItem1, typeItem2, repOffset);
        }
    }

    class UE3DelegateClassPropertyReader: UE3ClassPropertyReader
    {
        protected override UnClassProperty CreateProperty(UnExport export, int arraySize, FlagValues flags, int category, BinaryReader reader, short repOffset)
        {
            int typeId1 = reader.ReadInt32();
            reader.ReadInt32();
            var typeItem1 = export.Package.ResolveClassItem(typeId1);
            return new UnDelegateClassProperty(export, arraySize, flags, category, typeItem1, repOffset);
        }
    }
}
