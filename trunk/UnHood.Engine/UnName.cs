using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnHood.Engine
{
    public class UnName
    {
        private readonly string _name;
        private readonly long _flags;

        public UnName(string name, long flags)
        {
            _name = name;
            _flags = flags;
        }

        public string Name
        {
            get { return _name; }
        }

        public long Flags
        {
            get { return _flags; }
        }
    }
}
