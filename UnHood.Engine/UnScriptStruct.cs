using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnHood.Engine
{
    class UnScriptStruct: UnContainer
    {
        private readonly int _flags;

        public UnScriptStruct(UnExport self, int flags, int superIndex)
            : base(self, superIndex, null)
        {
            _flags = flags;
        }

        internal bool Native { get { return (_flags & 0x1) != 0; } }
        bool Transient { get { return (_flags & 0x8) != 0; } }

        public string Name { get { return _self.ObjectName; } }
        public UnPackageItem Super { get { return _super; } }

        public override void Decompile(TextBuilder result)
        {
            result.Indent().Append("struct ");
            if (Native) result.Append("native ");
            if (Transient) result.Append("transient ");
            result.Append(_self.ObjectName);
            if (_super != null)
            {
                result.Append(" extends ").Append(_super.ObjectName);
            }
            result.NewLine();
            result.Append("{").NewLine();
            result.PushIndent();
            DecompileChildren(result, true);
            result.PopIndent();
            result.Indent().Append("}").NewLine().NewLine();
        }
    }
}
