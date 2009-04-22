using System;
using System.IO;
using System.Linq;
using System.Text;
using UnHood.Engine;
using UnHood.Engine.UE3;

namespace UnDump
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: undump <filename|directory> <directory> [/scriptsource]");
                return;
            }

            bool scriptSource = (args.Length > 2 && args[2] == "/scriptsource");
            if (Directory.Exists(args [0]))
            {
                DumpAllPackages(args [0], args[0], args[1], scriptSource);
            }
            else
            {
                DumpPackage(args[0], Path.GetDirectoryName(args[0]), args[1], scriptSource);
            }
        }

        private static void DumpAllPackages(string gameDir, string packageDir, string outDir, bool scriptSource)
        {
            var entries = Directory.GetFileSystemEntries(packageDir);
            foreach(var entry in entries)
            {
                var fullPath = Path.Combine(packageDir, entry);
                if (Directory.Exists(fullPath))
                {
                    DumpAllPackages(gameDir, fullPath, outDir, scriptSource);
                }
                else
                {
                    var ext = Path.GetExtension(fullPath);
                    if (ext.Equals(".u", StringComparison.InvariantCultureIgnoreCase) /* || 
                        ext.Equals(".upk", StringComparison.InvariantCultureIgnoreCase)*/)
                    {
                        Console.WriteLine("Processing " + fullPath);
                        DumpPackage(fullPath, gameDir, Path.Combine(outDir, Path.GetFileNameWithoutExtension(fullPath)), scriptSource);
                    }
                }
            }
        }

        private static void DumpPackage(string packageName, string gameDir, string outDir, bool scriptSource)
        {
            using (Stream s = new FileStream(packageName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var package = UE3PackageReader.ReadPackage(new BinaryReader(s, Encoding.ASCII), gameDir);
                foreach (var export in package.TopLevelExports)
                {
                    if (export.ClassName == "Class")
                    {
                        Directory.CreateDirectory(outDir);
                        if (scriptSource)
                        {
                            var scriptText = export.Children.SingleOrDefault(e => e.ObjectName == "ScriptText");
                            if (scriptText != null)
                            {
                                var text = (string) scriptText.ReadInstance();
                                DumpOutput(export, outDir, text);
                                continue;
                            }
                        }
                        var instance = (UnClass)export.ReadInstance();
                        var builder = new TextBuilder();
                        instance.Decompile(builder);
                        DumpOutput(export, outDir, builder.ToString());
                    }
                }

                ProblemRegistry.LogProblems(Path.Combine(outDir, "unhood.log"));
            }
        }

        private static void DumpOutput(UnExport export, string outDir, string text)
        {
            var outPath = Path.Combine(outDir, export.ObjectName + ".uc");
            using (var fs = new FileStream(outPath, FileMode.Create))
            {
                using (var writer = new StreamWriter(fs, Encoding.UTF8))
                {
                    writer.Write(text);
                }
            }
        }
    }
}
