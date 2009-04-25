using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnHood.Engine.UE3
{
    internal class UE3FunctionReader : InstanceReader
    {
        private static readonly FlagSet _flagSet = new FlagSet("Final", "Defined", "Iterator", "Latent", 
            "PreOperator", "Singular", "Net", "NetReliable", 
            "Simulated", "Exec", "Native", "Event", 
            "Operator", "Static", "Const", null,
            null, "Public", "Private", "Protected", 
            "Delegate", "NetServer", "HasOutParms", "HasDefaults", 
            "NetClient", "FuncInherit", "FuncOverrideMatch");

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
            if ((functionFlags & _flagSet.GetMask("Net")) != 0)
            {
                reader.ReadInt16();  // repOffset
            }
            int friendlyNameIndex = reader.ReadInt32();
            reader.ReadInt32();
            return new UnFunction(export, package.Names[friendlyNameIndex].Name, 
                new FlagValues(functionFlags, _flagSet), bytecode, nativeIndex, operatorPrecedence);
        }
    }
}
    