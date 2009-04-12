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
                Console.WriteLine("Usage: undump <filename> <directory>");
                return;
            }
            using (Stream s = new FileStream(args[0], FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var package = UE3PackageReader.ReadPackage(new BinaryReader(s, Encoding.ASCII),
                                                           Path.GetDirectoryName(args[0]));
                foreach (var export in package.TopLevelExports)
                {
                    if (export.ClassName == "Class")
                    {
                        var instance = (UnClass) export.ReadInstance();
                        var builder = new TextBuilder();
                        instance.Decompile(builder);
                        var outPath = Path.Combine(args[1], export.ObjectName + ".uc");
                        using (var fs = new FileStream(outPath, FileMode.Create))
                        {
                            using (var writer = new StreamWriter(fs, Encoding.UTF8))
                            {
                                writer.Write(builder.ToString());
                            }
                        }
                    }
                }

                ProblemRegistry.LogProblems(Path.Combine(args[1], "unhood.log"));
            }
        }
    }
}
