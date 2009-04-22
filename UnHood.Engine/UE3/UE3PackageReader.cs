using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnHood.Engine.UE3
{
    public class UE3PackageReader: PackageReader
    {
        private string _baseDir;

        public UE3PackageReader(string baseDir)
        {
            _baseDir = baseDir;
        }

        public static UnPackage ReadPackage(BinaryReader reader, string baseDir)
        {
            UnPackage package = DoReadPackage(reader);
            package.LoadImportedDeclarations(new UE3PackageReader(baseDir));
            return package;
        }

        private static UnPackage DoReadPackage(BinaryReader reader)
        {
            var signature = reader.ReadInt32();
            var packageVersion = reader.ReadInt32();
            var firstExportOffset = reader.ReadInt32();
            var folderNameLength = reader.ReadInt32();
            var folderName = reader.ReadBytes(folderNameLength);
            var packageFlags = reader.ReadInt32();
            var namesCount = reader.ReadInt32();
            var namesOffset = reader.ReadInt32();
            var exportsCount = reader.ReadInt32();
            var exportsOffset = reader.ReadInt32();
            var importsCount = reader.ReadInt32();
            var importsOffset = reader.ReadInt32();
            reader.ReadInt32();  // unknown
            var guid = reader.ReadBytes(16);
            var generationsCount = reader.ReadInt32();
            for (int i = 0; i < generationsCount; i++)
            {
                reader.ReadBytes(12);
            }
            var engineVersion = reader.ReadInt32();
            var cookerVersion = reader.ReadInt32();
            if (engineVersion == 3240)
            {
                reader.ReadBytes(28);
            }
            Stream dataStream;
            if ((packageFlags & 0x00800000) != 0)
            {
                var compressionFlag = reader.ReadInt32();
                var chunkedStream = new ChunkedStream(reader.BaseStream);
                var chunkCount = reader.ReadInt32();
                for(int i=0; i<chunkCount; i++)
                {
                    var uncompressedOffset = reader.ReadInt32();
                    var uncompressedSize = reader.ReadInt32();
                    var compressedOffset = reader.ReadInt32();
                    var compressedSize = reader.ReadInt32();
                    chunkedStream.AddChunk(uncompressedOffset, uncompressedSize, compressedOffset, compressedSize);
                }
                dataStream = chunkedStream;
            }
            else
            {
                dataStream = reader.BaseStream;
            }
            UnPackage package = new UnPackage(dataStream);
            RegisterInstanceReaders(package);
            ReadNames(package, dataStream, namesOffset, namesCount);
            ReadImports(package, dataStream, importsOffset, importsCount);
            ReadExports(package, dataStream, exportsOffset, exportsCount);
            return package;
        }

        public UnPackage ReadPackage(string name)
        {
            Stream s = new FileStream(Path.Combine(_baseDir, name + ".u"), FileMode.Open, FileAccess.Read,
                                      FileShare.Read);
            return DoReadPackage(new BinaryReader(s));
        }

        private static void ReadNames(UnPackage package, Stream stream, int offset, int count)
        {
            stream.Position = offset;
            var reader = new BinaryReader(stream);
            for(int i=0; i<count; i++)
            {
                var nameLength = reader.ReadInt32();
                string name;
                if (nameLength < 0)
                {
                    var bytes = reader.ReadBytes(-2*nameLength);
                    name = new string(Encoding.Unicode.GetChars(bytes)).Trim('\0');
                }
                else
                {
                    name = new string(reader.ReadChars(nameLength)).Trim('\0');
                }
                var flags = reader.ReadInt64();
                package.AddName(name, flags);
            }
        }

        private static void ReadImports(UnPackage package, Stream stream, int offset, int count)
        {
            stream.Position = offset;
            var reader = new BinaryReader(stream);
            for (int i = 0; i < count; i++)
            {
                var packageNameIndex = reader.ReadInt32();
                reader.ReadInt32();
                var classNameIndex = reader.ReadInt32();
                reader.ReadInt32();
                var outer = reader.ReadInt32();
                var objectNameIndex = reader.ReadInt32();
                var flags = reader.ReadInt32();
                package.AddImport(packageNameIndex, classNameIndex, outer, objectNameIndex, flags);
            }
        }

        private static void ReadExports(UnPackage package, Stream stream, int offset, int count)
        {
            stream.Position = offset;
            var reader = new BinaryReader(stream);
            for (int i = 0; i < count; i++)
            {
                var classIndex = reader.ReadInt32();
                var superIndex = reader.ReadInt32();
                var outerIndex = reader.ReadInt32();
                var objectNameIndex = reader.ReadInt32();
                reader.ReadInt32();  // exportNameSuffix
                reader.ReadInt32();  // archetypeIndex
                var flags = reader.ReadInt64();
                var exportSize = reader.ReadInt32();
                var exportOffset = reader.ReadInt32();
                var componentMapCount = reader.ReadInt32();
                for (int j = 0; j < componentMapCount*3; j++)
                {
                    reader.ReadInt32();
                }
                var exportFlags = reader.ReadInt32();
                var netObjectCount = reader.ReadInt32();
                for (int j = 0; j < netObjectCount; j++)
                {
                    reader.ReadInt32();
                }
                if (netObjectCount == 0)
                {
                    reader.ReadInt32();
                }
                for(int j=0; j<4; j++)
                {
                    reader.ReadInt32();  // GUID
                }
                if (netObjectCount > 0)
                {
                    reader.ReadInt32();
                }
                package.AddExport(objectNameIndex, classIndex, outerIndex, exportOffset, exportSize);
            }
            package.ResolveExports();
        }

        private static void RegisterInstanceReaders(UnPackage package)
        {
            package.PropertyListReader = new UE3PropertyListReader();
            package.RegisterInstanceReader("Class", new UE3ClassReader());
            package.RegisterInstanceReader("Function", new UE3FunctionReader());
            package.RegisterInstanceReader("IntProperty", new UE3ClassPropertyReader());
            package.RegisterInstanceReader("FloatProperty", new UE3ClassPropertyReader());
            package.RegisterInstanceReader("StrProperty", new UE3ClassPropertyReader());
            package.RegisterInstanceReader("BoolProperty", new UE3ClassPropertyReader());
            package.RegisterInstanceReader("NameProperty", new UE3ClassPropertyReader());
            package.RegisterInstanceReader("ObjectProperty", new UE3StructPropertyReader());
            package.RegisterInstanceReader("ByteProperty", new UE3TypedClassPropertyReader());
            package.RegisterInstanceReader("ComponentProperty", new UE3TypedClassPropertyReader());
            package.RegisterInstanceReader("StructProperty", new UE3StructPropertyReader());
            package.RegisterInstanceReader("ClassProperty", new UE3ClassClassPropertyReader());
            package.RegisterInstanceReader("ArrayProperty", new UE3ArrayClassPropertyReader());
            package.RegisterInstanceReader("DelegateProperty", new UE3DelegateClassPropertyReader());
            package.RegisterInstanceReader("Enum", new UE3EnumReader());
            package.RegisterInstanceReader("Const", new UE3ConstReader());
            package.RegisterInstanceReader("ScriptStruct", new UE3ScriptStructReader());
            package.RegisterInstanceReader("State", new UE3StateReader());
            package.RegisterInstanceReader("TextBuffer", new UE3TextBufferReader());
        }
    }

    internal class UE3TextBufferReader : InstanceReader
    {
        public object ReadInstance(UnPackage package, BinaryReader reader, UnExport export)
        {
            reader.ReadBytes(20);
            int textLength = reader.ReadInt32();
            return new string(Encoding.Default.GetChars(reader.ReadBytes(textLength)));
        }
    }
}
