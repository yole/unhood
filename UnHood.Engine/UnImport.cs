using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnHood.Engine
{
    public class UnImport: UnPackageItem
    {
        private readonly UnName _packageName;
        protected UnName _className;
        private int _outer;
        private int _flags;

        public UnImport(UnName packageName, UnName className, int outer, UnName objectName, int flags)
        {
            _packageName = packageName;
            _className = className;
            _outer = outer;
            _objectName = objectName;
            _flags = flags;
        }

        public UnName PackageName
        {
            get { return _packageName; }
        }

        public override string ClassName
        {
            get { return _className.Name; }
        }

        public override string ToString()
        {
            return _objectName.Name + " (" + _packageName.Name + "." + _className.Name + 
                ") [" + _flags.ToString("X8") + "]";
        }
    }
}
