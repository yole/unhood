using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnHood.Engine.UE3
{
    class UE3StateReader: InstanceReader
    {
        public object ReadInstance(UnPackage package, BinaryReader reader, UnExport export)
        {
            reader.ReadBytes(40);
            int scriptSize = reader.ReadInt32();
            byte[] script = reader.ReadBytes(scriptSize);
            reader.ReadBytes(22);
            int methodCount = reader.ReadInt32();
            for(int i=0; i<methodCount; i++)
            {
                reader.ReadBytes(12);
            }
            return new UnState(export, script);
        }
    }
}
