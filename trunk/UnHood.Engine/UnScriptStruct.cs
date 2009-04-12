using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnHood.Engine
{
    class UnScriptStruct: Decompilable
    {
        private readonly UnExport _export;
        private readonly int _flags;

        public UnScriptStruct(UnExport export, int flags)
        {
            _export = export;
            _flags = flags;
        }

        bool Native { get { return (_flags & 0x1) != 0; } }
        bool Transient { get { return (_flags & 0x8) != 0; } }

        public void Decompile(TextBuilder result)
        {
            result.Indent().Append("struct ");
            if (Native) result.Append("native ");
            if (Transient) result.Append("transient ");
            result.Append(_export.ObjectName).NewLine();
            result.Append("{").NewLine();
            result.PushIndent();
            foreach (UnExport child in _export.Children.Reverse())
            {
                var instance = child.ReadInstance();
                if (instance is Decompilable)
                {
                    ((Decompilable) instance).Decompile(result);
                }
                else
                {
                    result.Indent().Append("// ").Append(child.ClassName).Append(" ").Append(child.ObjectName).NewLine();
                }
            }
            result.PopIndent();
            result.Indent().Append("}").NewLine().NewLine();
        }
    }
}
