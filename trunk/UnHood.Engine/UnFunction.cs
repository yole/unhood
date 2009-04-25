using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace UnHood.Engine
{
    public class UnFunction: UnBytecodeOwner
    {
        private readonly string _name;
        private readonly FlagValues _flags;
        private readonly int _nativeIndex;
        private readonly int _operatorPrecedence;

        internal UnFunction(UnExport export, string name, FlagValues flags, byte[] bytecode, int nativeIndex, int operatorPrecedence)
            : base(export, bytecode)
        {
            _name = name;
            _flags = flags;
            _nativeIndex = nativeIndex;
            _operatorPrecedence = operatorPrecedence;
        }

        public byte[] Bytecode
        {
            get { return _bytecode; }
        }

        internal string Name { get { return _name; } }
        internal int NativeIndex { get { return _nativeIndex; } }
        internal bool Native { get { return HasFlag("Native"); } }
        internal bool Event { get { return HasFlag("Event"); } }
        internal bool PreOperator { get { return HasFlag("PreOperator"); } }
        internal bool Operator { get { return HasFlag("Operator"); } }
        internal bool PostOperator { get { return Operator && _operatorPrecedence == 0; } }

        public override void Decompile(TextBuilder result)
        {
            Decompile(result, true);
        }

        internal bool HasFlag(string name)
        {
            return _flags.HasFlag(name);
        }

        public void Decompile(TextBuilder result, bool createControlStatements)
        {
            result.Indent();
            if (Native)
            {
                result.Append("native");
                if (_nativeIndex > 0)
                    result.Append("(").Append(_nativeIndex).Append(")");
                result.Append(" ");
            }

            _flags.Except("Native", "Event", "Delegate", "Defined", "Public", "HasDefaults", "HasOutParms").Each(f => result.Append(f.ToLower() + " "));

            if (HasFlag("Event"))
                result.Append("event ");
            else if (HasFlag("Delegate"))
                result.Append("delegate ");
            else
                result.Append("function ");
            string type = GetReturnType();
            if (type != null)
            {
                result.Append(type).Append(" ");
            }
            result.Append(_self.ObjectName).Append("(");
            int paramCount = 0;
            var locals = new List<UnClassProperty>();

            var statements = ReadBytecode();
            foreach (UnExport export in _self.Children.Reverse())
            {
                object instance = export.ReadInstance();
                if (instance is UnClassProperty)
                {
                    var prop = (UnClassProperty)instance;
                    if (prop.Parm)
                    {
                        if (!prop.ReturnParm)
                        {
                            if (paramCount > 0)
                                result.Append(", ");

                            prop.Flags.Each(f => result.Append(f.ToLower() + " "));
                            result.Append(prop.GetPropertyType()).Append(" ").Append(export.ObjectName);
                            if (prop.OptionalParm && statements.Count > 0)
                            {
                                if (statements[0].Token is NothingToken)
                                    statements.RemoveRange(0, 1);
                                else if (statements [0].Token is DefaultParamValueToken)
                                {
                                    result.Append(" = ").Append(statements[0].Token.ToString());
                                    statements.RemoveRange(0, 1);
                                }
                            }
                            paramCount++;
                        }
                    }
                    else
                    {
                        locals.Add(prop);
                    }
                }
            }
            result.Append(")");
            if (HasFlag("Defined"))
            {
                result.NewLine().Indent().Append("{").NewLine();
                result.PushIndent();
                foreach (var local in locals)
                {
                    result.Indent().Append("local ").Append(local.GetPropertyType()).Append(" ").Append(local.Name).Append(";").NewLine();
                }
                result.PopIndent();   // will be pushed again in DecompileBytecode()
                DecompileBytecode(statements, result, createControlStatements);
                result.Indent().Append("}").NewLine().NewLine();
            }
            else
            {
                result.Append(";").NewLine().NewLine();
            }
        }

        private string GetReturnType()
        {
            UnExport returnValue = _self.Children.SingleOrDefault(e => e.ObjectName == "ReturnValue");
            if (returnValue != null)
            {
                var prop = (UnClassProperty) returnValue.ReadInstance();
                return prop == null ? "???<" + returnValue.ClassName + ">" : prop.GetPropertyType();
            }
            return null;
        }
    }
}
