using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnHood.Engine.UE3
{
    internal class UE3FunctionReader : InstanceReader
    {
        public object ReadInstance(UnPackage package, BinaryReader reader, UnExport export)
        {
            reader.ReadBytes(12);
            int super = reader.ReadInt32();
            int children = reader.ReadInt32();
            reader.ReadBytes(12);
            int line = reader.ReadInt32();
            int textPos = reader.ReadInt32();
            int scriptSize = reader.ReadInt32();
            byte[] bytecode = reader.ReadBytes(scriptSize);
            int nativeIndex = reader.ReadInt16();
            int operatorPrecedence = reader.ReadByte();
            int functionFlags = reader.ReadInt32();
            if ((functionFlags & UnFunction.FF_NET) != 0)
            {
                reader.ReadInt16();  // repOffset
            }
            int friendlyNameIndex = reader.ReadInt32();
            reader.ReadInt32();
            return new UnFunction(export, package.Names[friendlyNameIndex].Name, functionFlags, bytecode, nativeIndex, operatorPrecedence);
        }
    }
}
    