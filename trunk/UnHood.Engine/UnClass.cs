using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace UnHood.Engine
{
    public abstract class UnBytecodeOwner: Decompilable
    {
        protected readonly UnExport _self;
        protected readonly byte[] _bytecode;

        protected UnBytecodeOwner(UnExport self, byte[] bytecode)
        {
            _self = self;
            _bytecode = bytecode;
        }

        public UnExport Export { get { return _self; } }
        public UnPackage Package { get { return _self.Package; } }

        public abstract void Decompile(TextBuilder result);

        protected StatementList ReadBytecode()
        {
            var s = new MemoryStream(_bytecode);
            var reader = new BinaryReader(s);
            var bcReader = new BytecodeReader(_self.Package, reader);
            var statements = new StatementList((Statement) null);
            while(s.Position < s.Length)
            {
                int startOffset = (int) s.Position;
                BytecodeToken bc;
                try
                {
                    bc = bcReader.ReadNext();
                }
                catch (EndOfStreamException)
                {
                    break;
                }
                if (bc == null) break;
                statements.Add(new Statement(startOffset, (int) s.Position, bc));
                if (bc is ErrorBytecodeToken)
                {
                    var errorToken = (ErrorBytecodeToken) bc;
                    int bytecode = errorToken.UnknownBytecode;
                    if (bytecode >= 0)
                    {
                        ProblemRegistry.RegisterUnknownBytecode((byte) bytecode, this, errorToken.SubsequentBytes);
                    }
                    else
                    {
                        ProblemRegistry.RegisterBytecodeError(this, errorToken.ToString());
                    }
                    break;
                }
            }
            return statements;
        }

        protected void DecompileBytecode(StatementList statements, TextBuilder result, bool createControlStatements)
        {
            var labelTableStatement = statements.Find(s => s.Token is LabelTableToken);
            var labelTable = labelTableStatement != null ? (LabelTableToken) labelTableStatement.Token : null;
            result.HasErrors = statements.HasErrors();
            if (createControlStatements)
            {
                statements.CreateControlStatements();
                if (statements.IsIncompleteControlFlow())
                {
                    ProblemRegistry.RegisterIncompleteControlFlow(this);
                }
                statements.RemoveRedundantReturns();
            }
            statements.Print(result, labelTable, !createControlStatements);
        }
    }

    public abstract class UnContainer: UnBytecodeOwner
    {
        protected UnContainer(UnExport self, byte[] bytecode) : base(self, bytecode)
        {
        }

        protected void DecompileChildren(TextBuilder result)
        {
            foreach (UnExport export in _self.Children)
            {
                try
                {
                    object instance = export.ReadInstance();
                    if (instance is Decompilable)
                    {
                        ((Decompilable)instance).Decompile(result);
                    }
                    else
                    {
                        result.Append("// ").Append(export.ToString()).Append("\n");
                    }
                }
                catch (Exception e)
                {
                    result.Append("//!!! Error decompiling " + export.ObjectName + ": " + e.Message);
                }
            }
        }
    }

    public class UnClass: UnContainer
    {
        private readonly UnPackageItem _super;

        public UnClass(UnExport self, int superIndex, byte[] bytecode)
            : base(self, bytecode)
        {
            _super = _self.Package.ResolveClassItem(superIndex);
        }

        public override void Decompile(TextBuilder result)
        {
            result.Append("class ").Append(_self.ObjectName);
            if (_super != null)
                result.Append(" extends ").Append(_super.ObjectName);
            result.Append(";\n");
            DecompileChildren(result);

            var statementList = ReadBytecode();
            if (statementList.Count > 0)
            {
                result.Append("replication\n{\n").PushIndent();
                for(int i=0; i<statementList.Count; i++)
                {
                    List<String> names = FindReplicatedProperties(statementList [i].StartOffset);
                    if (names.Count > 0)
                    {
                        result.Indent().Append("if (").Append(statementList[i].Token.ToString()).Append(")").NewLine();
                        result.Indent().Append("    ");
                        foreach (string name in names)
                        {
                            result.Append(name);
                            if (name != names.Last()) result.Append(", ");
                        }
                        result.Append(";").NewLine().NewLine();
                    }
                }
                result.Append("}").NewLine().PopIndent();
            }
        }

        private List<string> FindReplicatedProperties(int offset)
        {
            var result = new List<string>();
            foreach(UnExport export in _self.Children)
            {
                var instance = export.ReadInstance() as UnClassProperty;
                if (instance != null && instance.RepOffset == offset)
                {
                    result.Add(export.ObjectName);
                }
            }
            return result;
        }
    }
}
