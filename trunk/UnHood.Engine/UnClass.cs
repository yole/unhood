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
                if (bc == null || bc is EndOfScriptToken) break;
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
                try
                {
                    statements.CreateControlStatements();
                }
                catch (Exception)
                {
                    ProblemRegistry.RegisterIncompleteControlFlow(this);
                }
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
        protected readonly UnPackageItem _super;

        protected UnContainer(UnExport self, int superIndex, byte[] bytecode)
            : base(self, bytecode)
        {
            _super = superIndex == 0 ? null : _self.Package.ResolveClassItem(superIndex);
        }

        protected void DecompileChildren(TextBuilder result, bool reverse)
        {
            var collection = reverse ? _self.Children.Reverse() : _self.Children;
            foreach (UnExport export in collection)
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

        internal UnExport FindMemberExport(string name)
        {
            var export = _self.Children.SingleOrDefault(e => e.ObjectName == name);
            if (export != null) return export;

            var superExport = _super.Resolve();
            if (superExport != null)
            {
                var superClass = superExport.ReadInstance() as UnContainer;
                if (superClass != null)
                {
                    return superClass.FindMemberExport(name);
                }
            }
            return null;
        }
    }

    public class UnClass: UnContainer
    {
        private readonly UnPackageItem _outerInstance;
        private readonly FlagValues _flags;
        private readonly UnPackageItem _defaults;
        private readonly string _config;
        private readonly List<string> _hideCategories;
        private readonly List<UnPackageItem> _interfaces;

        internal UnClass(UnExport self, int superIndex, UnPackageItem outerInstance, byte[] bytecode, FlagValues flags, 
            UnPackageItem defaults, string config, List<string> hideCategories, List<UnPackageItem> interfaces)
            : base(self, superIndex, bytecode)
        {
            _outerInstance = outerInstance;
            _flags = flags;
            _defaults = defaults;
            _config = config;
            _hideCategories = hideCategories;
            _interfaces = interfaces;
        }

        public override void Decompile(TextBuilder result)
        {
            result.Append("class ").Append(_self.ObjectName);
            if (_super != null)
                result.Append(" extends ").Append(_super.ObjectName);
            if (_outerInstance != null && _outerInstance.ObjectName != "Object")
            {
                result.NewLine().Append("    within ").Append(_outerInstance.ObjectName);
            }
            if (_hideCategories.Count > 0)
            {
                result.NewLine().Append("    hidecategories(").Append(string.Join(",", _hideCategories.ToArray())).Append(")");
            }
            if (_interfaces.Count > 0)
            {
                var intfNames = _interfaces.ConvertAll(e => e.ObjectName).ToArray();
                result.NewLine().Append("    implements(").Append(string.Join(",", intfNames)).Append(")");
            }
            if (_config != "None")
            {
                result.NewLine().Append("    config(").Append(_config).Append(")");
            }
            _flags.Except("Compiled", "Parsed", "Config", "Localized").Each(f => result.NewLine().Append("    ").Append(f.ToLower()));
            result.Append(";").NewLine().NewLine();
            DecompileChildren(result, false);

            var statementList = ReadBytecode();
            if (statementList.Count > 0)
            {
                DecompileReplicationBlock(result, statementList);
            }
            if (_defaults != null)
            {
                DecompileDefaultProperties(result);
            }
        }

        private void DecompileReplicationBlock(TextBuilder result, StatementList statementList)
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
            result.Append("}").NewLine().NewLine().PopIndent();
        }

        private void DecompileDefaultProperties(TextBuilder result)
        {
            result.Append("defaultproperties\n{\n").PushIndent();
            var defaultsExport = _defaults.Resolve();
            UnPropertyList propertyList = Package.ReadPropertyList(defaultsExport, this);
            foreach(UnProperty prop in propertyList.Properties)
            {
                var name = prop.Name;
                if (name.StartsWith("__") && name.EndsWith("__Delegate"))
                {
                    name = name.Substring(2, name.Length - 2 - 10);
                }
                if (prop.Value is UnPropertyArray)
                {
                    var array = (UnPropertyArray) prop.Value;
                    for(int i=0; i<array.Count; i++)
                    {
                        result.Indent().Append(name).Append("(").Append(i).Append(")=")
                            .Append(ValueToString(array [i], array.ElementType)).NewLine();
                    }
                }
                else
                {
                    result.Indent().Append(name).Append("=").Append(ValueToString(prop.Value, prop.Type)).NewLine();
                }
            }
            foreach(UnExport export in defaultsExport.Children)
            {
                result.Indent().Append("// child object " + export.ObjectName + " of type " + export.ClassName).NewLine();
            }
            result.Append("}").NewLine().PopIndent();
        }

        private string ValueToString(object value, string type)
        {
            if (value == null)
                return "?";
            if (type == "BoolProperty")
                return (bool) value ? "true" : "false";
            if (type == "StrProperty")
                return "\"" + value + "\"";
            if (type == "StructProperty")
                return StructToString((UnPropertyList) value);
            return value.ToString();
        }

        private string StructToString(UnPropertyList value)
        {
            if (value == null) return "?";
            var result = value.Properties.Aggregate("", (s, prop) => s + "," + prop.Name + "=" + ValueToString(prop.Value, prop.Type));
            return "(" + (result.Length > 0 ? result.Substring(1) : result) + ")";
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
