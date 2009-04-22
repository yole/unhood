using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnHood.Engine.UE3
{
    class UE3ClassReader: InstanceReader
    {
        public object ReadInstance(UnPackage package, BinaryReader reader, UnExport export)
        {
            reader.ReadInt32();
            int superIndex = reader.ReadInt32();
            reader.ReadInt32();
            int scriptTextIndex = reader.ReadInt32();
            for (int i = 0; i < 4; i++)
                reader.ReadInt32();
            int scriptSize = reader.ReadInt32();
            var bytecode = reader.ReadBytes(scriptSize);
            for (int i = 0; i < 4; i++)
                reader.ReadInt32();
            reader.ReadInt16();
            reader.ReadInt32();
            int c1 = reader.ReadInt32();
            for (int i = 0; i < c1 * 3; i++)
                reader.ReadInt32();
            reader.ReadByte();
            for (int i = 0; i < 4; i++)
                reader.ReadInt32();
            int c2 = reader.ReadInt32();
            for (int i = 0; i < c2 * 2; i++)
                reader.ReadInt32();
            int c3 = reader.ReadInt32();
            for (int i = 0; i < c3 * 3; i++)
                reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            int defaultPropertiesIndex = reader.ReadInt32();
            UnExport defaultProperties = defaultPropertiesIndex == 0
                ? null : package.ResolveClassItem(defaultPropertiesIndex).Resolve();
            return new UnClass(export, superIndex, bytecode, defaultProperties);
        }
    }
}
