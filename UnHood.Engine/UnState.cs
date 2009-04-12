using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnHood.Engine
{
    class UnState: UnContainer
    {
        public UnState(UnExport self, byte[] bytecode)
            : base(self, bytecode)
        {
        }

        public override void Decompile(TextBuilder result)
        {
            result.Indent().Append("state ").Append(_self.ObjectName).NewLine();
            result.Indent().Append("{").NewLine();
            result.PushIndent();
            DecompileChildren(result);
            result.PopIndent();

            StatementList bytecode = ReadBytecode();
            DecompileBytecode(bytecode, result, true);

            result.Indent().Append("}").NewLine().NewLine();
        }
    }
}
