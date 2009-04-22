using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnHood.Engine.UE3
{
    class UE3ScriptStructReader: InstanceReader
    {
        public object ReadInstance(UnPackage package, BinaryReader reader, UnExport export)
        {
            int[] values = new int[11];
            for (int i = 0; i < 11; i++)
                values[i] = reader.ReadInt32();
            int flags = reader.ReadInt32();
            return new UnScriptStruct(export, flags, values [3]);
        }
    }
}
