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
            int methodCount = reader.ReadInt32();
            for (int i = 0; i < methodCount * 3; i++)
                reader.ReadInt32();
            reader.ReadByte();
            for (int i = 0; i < 2; i++)
                reader.ReadInt32();
            var configNameIndex = reader.ReadInt64();
            var config = package.Names[(int) configNameIndex].Name;
            int hideCategoriesCount = reader.ReadInt32();
            var hideCategories = new List<string>();
            for (int i = 0; i < hideCategoriesCount; i++)
            {
                var nameIndex = reader.ReadInt64();
                var name = package.Names[(int) nameIndex].Name;
                hideCategories.Add(name);
            }
            int c3 = reader.ReadInt32();
            for (int i = 0; i < c3 * 3; i++)
                reader.ReadInt32();
            var interfaceCount = reader.ReadInt32();
            var interfaces = new List<UnPackageItem>();
            for(int i=0; i<interfaceCount; i++)
            {
                var intfIndex = reader.ReadInt32();
                reader.ReadInt32();
                interfaces.Add(package.ResolveClassItem(intfIndex));
            }
            reader.ReadInt32();
            int defaultPropertiesIndex = reader.ReadInt32();
            UnExport defaultProperties = defaultPropertiesIndex == 0
                ? null : package.ResolveClassItem(defaultPropertiesIndex).Resolve();
            return new UnClass(export, superIndex, bytecode, defaultProperties, config, hideCategories, interfaces);
        }
    }
}
