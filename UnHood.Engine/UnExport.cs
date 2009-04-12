using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace UnHood.Engine
{
    public class UnExport: UnPackageItem
    {
        private UnPackage _package;
        private int _exportOffset;
        private int _exportSize;
        private readonly List<UnExport> _children = new List<UnExport>();
        private UnExport _parent;

        public UnExport(UnPackage package, UnName objectName, int exportOffset, int exportSize)
        {
            _package = package;
            _objectName = objectName;
            _exportOffset = exportOffset;
            _exportSize = exportSize;
        }

        public override string ToString()
        {
            return _objectName.Name + ": " + ClassName + "[" + _exportSize + "]";
        }

        public override string ClassName
        {
           get { return (ClassItem == null ? "Class" : ClassItem.ObjectName); }
        }

        public UnPackageItem ClassItem { get; set; }

        public int ExportOffset { get { return _exportOffset; } }
        public UnPackage Package { get { return _package;  } }
        public UnExport Parent { get { return _parent; } }

        internal void AddChild(UnExport export)
        {
            _children.Add(export);
            export._parent = this;
        }

        public ReadOnlyCollection<UnExport> Children
        {
            get { return _children.AsReadOnly(); }
        }

        public object ReadInstance()
        {
            return _package.ReadInstance(ClassName, this);
        }
    }
}
