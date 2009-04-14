using System;
using System.IO;
using System.Text;
using UnHood.Engine;
using UnHood.Engine.UE3;

namespace UnDump
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: undump <filename|directory> <directory>");
                return;
            }

            if (Directory.Exists(args [0]))
            {
                DumpAllPackages(args [0], args[0], args[1]);
            }
            else
            {
                DumpPackage(args[0], Path.GetDirectoryName(args[0]), args[1]);
            }
        }

        private static void DumpAllPackages(string gameDir, string packageDir, string outDir)
        {
            var entries = Directory.GetFileSystemEntries(packageDir);
            foreach(var entry in entries)
            {
                var fullPath = Path.Combine(packageDir, entry);
                if (Directory.Exists(fullPath))
                {
                    DumpAllPackages(gameDir, fullPath, outDir);
                }
                else
                {
                    var ext = Path.GetExtension(fullPath);
                    if (ext.Equals(".u", StringComparison.InvariantCultureIgnoreCase) || 
                        ext.Equals(".upk", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine("Processing " + fullPath);
                        DumpPackage(fullPath, gameDir, Path.Combine(outDir, Path.GetFileNameWithoutExtension(fullPath)));
                    }
                }
            }
        }

        private static void DumpPackage(string packageName, string gameDir, string outDir)
        {
            using (Stream s = new FileStream(packageName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var package = UE3PackageReader.ReadPackage(new BinaryReader(s, Encoding.ASCII), gameDir);
                foreach (var export in package.TopLevelExports)
                {
                    if (export.ClassName == "Class")
                    {
                        Directory.CreateDirectory(outDir);
                        var instance = (UnClass) export.ReadInstance();
                        var builder = new TextBuilder();
                        instance.Decompile(builder);
                        var outPath = Path.Combine(outDir, export.ObjectName + ".uc");
                        using (var fs = new FileStream(outPath, FileMode.Create))
                        {
                            using (var writer = new StreamWriter(fs, Encoding.UTF8))
                            {
                                writer.Write(builder.ToString());
                            }
                        }
                    }
                }

                ProblemRegistry.LogProblems(Path.Combine(outDir, "unhood.log"));
            }
        }
    }
}
