using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnHood.Engine.UE3
{
    class UE3EnumReader: InstanceReader
    {
        public object ReadInstance(UnPackage package, BinaryReader reader, UnExport export)
        {
            reader.ReadBytes(20);
            int valueCount = reader.ReadInt32();
            var values = new List<string>();
            for(int i=0; i<valueCount; i++)
            {
                int index = (int) reader.ReadInt64();
                values.Add(export.Package.Names[index].Name);
            }
            return new UnEnum(export, values);
        }
    }
}
