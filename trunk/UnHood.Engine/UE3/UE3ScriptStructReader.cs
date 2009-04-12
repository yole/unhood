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
            reader.ReadBytes(11*4);
            int flags = reader.ReadInt32();
            return new UnScriptStruct(export, flags);
        }
    }
}
