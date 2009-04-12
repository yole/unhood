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
        private readonly int _flags;
        private readonly int _nativeIndex;
        private readonly int _operatorPrecedence;

        private const int FF_FINAL = 1;
        private const int FF_DEFINED = 2;
        private const int FF_LATENT = 8;
        private const int FF_PRE_OPERATOR = 0x10;
        internal const int FF_NET = 0x40;
        private const int FF_NETRELIABLE = 0x80;
        private const int FF_SIMULATED = 0x100;
        private const int FF_EXEC = 0x200;
        private const int FF_NATIVE = 0x400;
        private const int FF_EVENT = 0x800;
        private const int FF_OPERATOR = 0x1000;
        private const int FF_STATIC = 0x2000;
        private const int FF_PROTECTED = 0x80000;
        private const int FF_DELEGATE = 0x100000;

        public UnFunction(UnExport export, string name, int flags, byte[] bytecode, int nativeIndex, int operatorPrecedence)
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

        internal bool Final { get { return (_flags & FF_FINAL) != 0; } }
        internal bool Defined { get { return (_flags & FF_DEFINED) != 0; } }
        internal bool Latent { get { return (_flags & FF_LATENT) != 0; } }
        internal bool Simulated { get { return (_flags & FF_SIMULATED) != 0; } }
        internal bool Exec { get { return (_flags & FF_EXEC) != 0; } }
        internal bool Native { get { return (_flags & FF_NATIVE) != 0; } }
        internal bool Event { get { return (_flags & FF_EVENT) != 0; } }
        internal bool PreOperator { get { return (_flags & FF_PRE_OPERATOR) != 0; } }
        internal bool Operator { get { return (_flags & FF_OPERATOR) != 0; } }
        internal bool PostOperator { get { return Operator && _operatorPrecedence == 0; } }
        internal bool Static { get { return (_flags & FF_STATIC) != 0; } }
        internal bool Protected { get { return (_flags & FF_PROTECTED) != 0; } }
        internal bool Delegate { get { return (_flags & FF_DELEGATE) != 0; } }
        internal bool Reliable { get { return (_flags & FF_NETRELIABLE) != 0; } }

        internal string Name { get { return _name; } }
        internal int NativeIndex { get { return _nativeIndex; } }

        public override void Decompile(TextBuilder result)
        {
            Decompile(result, true);
        }

        private int GetUnknownFunctionFlags()
        {
            return _flags & ~(FF_NATIVE | FF_FINAL | FF_LATENT | FF_SIMULATED | FF_EXEC | FF_STATIC | FF_EVENT | FF_PROTECTED | FF_DELEGATE | FF_DEFINED |
                FF_NETRELIABLE | 0x20000);
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
            if (Final) result.Append("final ");
            if (Latent) result.Append("latent ");
            if (Simulated) result.Append("simulated ");
            if (Exec) result.Append("exec ");
            if (Static) result.Append("static ");
            if (Protected) result.Append("protected ");
            if (Reliable) result.Append("reliable ");
            int remainingFunctionFlags = GetUnknownFunctionFlags();
            if (remainingFunctionFlags != 0)
            {
                result.Append("/* ").Append(remainingFunctionFlags.ToString("X8")).Append("*/ ");
            }

            if (Event)
                result.Append("event ");
            else if (Delegate)
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
                            if (prop.OutParm) result.Append("out ");
                            if (prop.OptionalParm) result.Append("optional ");
                            if (prop.CoerceParm) result.Append("coerce ");
                            if (prop.Const) result.Append("const ");
                            int remainingFlags = prop.GetUnknownParamFlags();
                            if (remainingFlags != 0)
                            {
                                result.Append("/* ").Append(remainingFlags.ToString("X8")).Append("*/ ");
                            }
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
            if (!Native && Defined)
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
