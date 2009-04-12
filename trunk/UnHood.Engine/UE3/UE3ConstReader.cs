using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnHood.Engine.UE3
{
    class UE3ConstReader: InstanceReader
    {
        public object ReadInstance(UnPackage package, BinaryReader reader, UnExport export)
        {
            reader.ReadBytes(20);
            int valueSize = reader.ReadInt32();
            string value = new string(reader.ReadChars(valueSize));
            return new UnConst(export, value.Trim('\0'));
        }
    }
}
