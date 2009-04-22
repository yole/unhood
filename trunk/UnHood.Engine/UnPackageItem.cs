using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnHood.Engine
{
    public abstract class UnPackageItem
    {
        protected UnName _objectName;

        public string ObjectName
        {
            get { return _objectName.Name; }
        }

        public abstract string ClassName
        { 
            get;
        }

        public abstract UnExport Resolve();
    }
}
