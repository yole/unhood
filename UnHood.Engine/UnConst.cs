using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnHood.Engine
{
    class UnConst: Decompilable
    {
        private readonly UnExport _export;
        private readonly string _value;

        public UnConst(UnExport export, string value)
        {
            _export = export;
            _value = value;
        }

        public void Decompile(TextBuilder result)
        {
            result.Append("const ").Append(_export.ObjectName).Append(" = ").Append(_value).Append(";").NewLine();
        }
    }
}
