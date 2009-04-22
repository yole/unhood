using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace UnHood.Engine
{
    interface PackageReader
    {
        UnPackage ReadPackage(string name);
    }

    interface InstanceReader
    {
        object ReadInstance(UnPackage package, BinaryReader reader, UnExport export);
    }

    interface PropertyListReader
    {
        UnPropertyList ReadPropertyList(UnPackage package, BinaryReader reader, UnExport export, UnClass owner);
    }

    public class UnPackage
    {
        private readonly Stream _stream;
        private readonly List<UnName> _names = new List<UnName>();
        private readonly List<UnImport> _imports = new List<UnImport>();
        private readonly List<UnExport> _exports = new List<UnExport>();
        private readonly List<UnExport> _topLevelExports = new List<UnExport>();
        private readonly Dictionary<string, UnPackage> _importedPackages = new Dictionary<string, UnPackage>();

        private readonly List<int> _exportClassIndexList = new List<int>();
        private readonly List<int> _outerIndexList = new List<int>();

        private readonly Dictionary<string, InstanceReader> _instanceReaders = new Dictionary<string, InstanceReader>();
        private readonly Dictionary<int, UnFunction> _nativeFunctions = new Dictionary<int, UnFunction>();

        public UnPackage(Stream stream)
        {
            _stream = stream;
        }

        public ReadOnlyCollection<UnName> Names
        {
            get { return _names.AsReadOnly(); }
        }

        public ReadOnlyCollection<UnImport> Imports
        {
            get { return _imports.AsReadOnly(); }
        }

        public ReadOnlyCollection<UnExport> Exports
        {
            get { return _exports.AsReadOnly(); }
        }

        public ReadOnlyCollection<UnExport> TopLevelExports
        {
            get { return _topLevelExports.AsReadOnly(); }
        }

        internal PropertyListReader PropertyListReader { get; set; }

        internal void RegisterInstanceReader(string className, InstanceReader reader)
        {
            _instanceReaders.Add(className, reader);
        }

        internal void AddName(string name, long flags)
        {
            _names.Add(new UnName(name, flags));
        }

        internal void AddImport(int packageNameIndex, int classNameIndex, int outer, 
            int nameIndex, int flags)
        {
            _imports.Add(new UnImport(this, _names [packageNameIndex], _names [classNameIndex],
                outer, _names [nameIndex], flags));
        }

        internal void AddExport(int objectNameIndex, int classIndex, int outerIndex, int exportOffset, int exportSize)
        {
            _exports.Add(new UnExport(this, _names [objectNameIndex], exportOffset, exportSize));
            _exportClassIndexList.Add(classIndex);
            _outerIndexList.Add(outerIndex);
        }

        internal void ResolveExports()
        {
            for(int i=0; i<_exports.Count; i++)
            {
                _exports[i].ClassItem = ResolveClassItem(_exportClassIndexList[i]);
                int outerIndex = _outerIndexList [i];
                if (outerIndex == 0)
                {
                    _topLevelExports.Add(_exports [i]);
                }
                else
                {
                    _exports [outerIndex-1].AddChild(_exports [i]);
                }
            }
            _exportClassIndexList.Clear();
        }

        internal UnPackageItem ResolveClassItem(int i)
        {
            if (i < 0)
                return _imports[-i - 1];
            if (i > 0 && i <= _exports.Count)
                return _exports[i - 1];
            return null;
        }

        public object ReadInstance(string className, UnExport export)
        {
            InstanceReader reader;
            if (!_instanceReaders.TryGetValue(className, out reader))
                return null;
            return ReadInstance(export, reader);
        }

        internal object ReadInstance(UnExport export, InstanceReader reader)
        {
            long oldPos = _stream.Position;
            try
            {
                _stream.Position = export.ExportOffset;
                return reader.ReadInstance(this, new BinaryReader(_stream), export);
            }
            finally
            {
                _stream.Position = oldPos;
            }
        }

        internal UnPropertyList ReadPropertyList(UnExport export, UnClass owner)
        {
            _stream.Position = export.ExportOffset;
            return PropertyListReader.ReadPropertyList(this, new BinaryReader(_stream), export, owner);
        }

        internal void LoadImportedDeclarations(PackageReader packageReader)
        {
            foreach (var import in _imports)
            {
                var name = import.PackageName.Name;
                if (!_importedPackages.ContainsKey(name))
                {
                    var package = packageReader.ReadPackage(name);
                    LoadNativeFunctions(package);
                    _importedPackages[name] = package;
                }
            }
            LoadNativeFunctions(this);
        }

        private void LoadNativeFunctions(UnPackage package)
        {
            for (int i = 0; i < package.Exports.Count; i++)
            {
                var export = package.Exports[i];
                if (export.ClassName == "Function")
                {
                    var function = (UnFunction) export.ReadInstance();
                    if (function.Native && !function.Event && function.NativeIndex != 0)
                    {
                        _nativeFunctions[function.NativeIndex] = function;
                    }
                }
            }
        }

        internal UnFunction GetNativeFunction(int index)
        {
            UnFunction result;
            if (!_nativeFunctions.TryGetValue(index, out result)) return null;
            return result;
        }

        public UnExport ResolveImport(UnImport import)
        {
            UnPackage importedPackage;
            if (!_importedPackages.TryGetValue(import.PackageName.Name, out importedPackage))
            {
                return null;
            }
            var candidates = importedPackage._exports.FindAll(e => e.ClassName == import.ClassName && e.ObjectName == import.ObjectName);
            if (candidates.Count == 1)
            {
                return candidates[0];
            }
            return null;
        }
    }
}
