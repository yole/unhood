using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnHood.Engine
{
    class UnEnum: Decompilable
    {
        private UnExport _export;
        private List<string> _values;

        public UnEnum(UnExport export, List<string> values)
        {
            _export = export;
            _values = values;
        }

        public void Decompile(TextBuilder result)
        {
            result.Append("enum ").Append(_export.ObjectName).Append("\n{\n");
            _values.GetRange(0, _values.Count-1).ForEach(v => result.Append("    ").Append(v).Append(",\n"));
            result.Append("};\n\n");
        }
    }
}
