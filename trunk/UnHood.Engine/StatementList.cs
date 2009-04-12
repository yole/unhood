using System;
using System.Collections.Generic;

namespace UnHood.Engine
{
    public class StatementList
    {
        private Statement _parent;
        private List<Statement> _statements = new List<Statement>();

        public StatementList()
        {
        }

        internal StatementList(Statement parent)
        {
            _parent = parent;
        }

        internal StatementList(List<Statement> statements)
        {
            _statements = statements;
        }

        internal void Add(Statement statement)
        {
            _statements.Add(statement);
            statement.Parent = _parent;
        }

        internal Statement Parent
        {
            get { return _parent; }
            set
            {
                _parent = value;
                _statements.ForEach(s => s.Parent = value);
            }
        }
        public int Count { get { return _statements.Count; } }
        internal Statement this[int index] { get { return _statements[index]; } }

        public void Print(TextBuilder result, LabelTableToken labelTable, bool showStartOffset)
        {
            result.PushIndent();
            _statements.ForEach(s => s.Print(result, labelTable, showStartOffset));
            result.PopIndent();
        }

        public int FindByTargetOffset(int offset, int startIndex)
        {
            if (startIndex >= _statements.Count) return -1;
            var composite = Parent as CompositeStatement;
            if (composite != null && composite.EndOffset == offset)
                return _statements.Count;
            return _statements.FindIndex(startIndex, s => s.StartOffset == offset);
        }

        internal void ReplaceRange(int startIndex, int count, Statement statement)
        {
            _statements.RemoveRange(startIndex, count);
            _statements.Insert(startIndex, statement);
            statement.Parent = _parent;
        }

        internal void ReplaceRange(int index, StatementList oldStatements, Statement newStatement)
        {
            oldStatements._statements.ForEach(c => _statements.Remove(c));
            _statements.Insert(index, newStatement);
        }

        public StatementList GetRange(int startIndex, int count)
        {
            List<Statement> statements = _statements.GetRange(startIndex, count);
            return new StatementList(statements);
        }

        internal Statement FindParent(Func<Statement, bool> func)
        {
            Statement stmt = _parent;
            while (stmt != null)
            {
                if (func(stmt)) return stmt;
                stmt = stmt.Parent;
            }
            return null;
        }

        internal Statement Find(Predicate<Statement> func)
        {
            return _statements.Find(func);
        }

        public void RemoveRange(int startIndex, int count)
        {
            _statements.RemoveRange(startIndex, count);
        }

        public void CreateControlStatements()
        {
            bool workToDo = true;
            while (workToDo)
            {
                workToDo = CreateControlStatementsPass();
            }
        }

        private bool CreateControlStatementsPass()
        {
            for (int i = 0; i < Count; i++)
            {
                var jumpIfNotToken = this[i].Token as JumpIfNotToken;
                if (jumpIfNotToken != null)
                {
                    int ifEnd = FindByTargetOffset(jumpIfNotToken.TargetOffset, 0);
                    if (ifEnd >= 0)
                    {
                        CreateIfStatement(i, jumpIfNotToken, ifEnd);
                        return true;
                    }
                }
                var jumpToken = this[i].Token as UncondJumpToken;
                if (jumpToken != null)
                {
                    var forStatement = FindBreakTarget(jumpToken.TargetOffset);
                    if (forStatement != null)
                    {
                        if (i > 0 && this [i-1].Token is IteratorNextToken)
                        {
                            ReplaceRange(i-1, 2, new BreakContinueStatement(this [i].StartOffset, jumpToken, "continue"));
                        }
                        else
                        {
                            ReplaceRange(i, 1, new BreakContinueStatement(this[i].StartOffset, jumpToken, "break"));
                        }
                        continue;
                    }
                }
                if (!(this[i] is ForeachStatement))
                {
                    var foreachToken = this[i].Token as ForeachToken;
                    if (foreachToken != null)
                    {
                        int foreachEnd = FindByTargetOffset(foreachToken.TargetOffset + 1, i);
                        if (foreachEnd >= 0)
                        {
                            CreateForeachStatement(i, foreachToken, foreachEnd);
                            return true;
                        }
                    }
                }
                if (!(this[i] is SwitchStatement))
                {
                    var switchToken = this[i].Token as SwitchToken;
                    if (switchToken != null)
                    {
                        return CreateSwitchStatement(i, switchToken);
                    }
                }
            }
            return false;
        }

        private Statement FindBreakTarget(int targetOffset)
        {
            return FindParent(s => s is ForeachStatement && ((ForeachStatement) s).EndOffset == targetOffset);
        }

        private void CreateIfStatement(int i, JumpIfNotToken jumpToken, int ifEnd)
        {
            int ifCount = ifEnd - i - 1;
            int ifEndOffset = jumpToken.TargetOffset;
            CompositeStatement elseStatement = null;
            bool isWhile = false;
            if (this[ifEnd - 1].Token is UncondJumpToken)
            {
                var endToken = (UncondJumpToken)this[ifEnd - 1].Token;
                if (endToken.TargetOffset == this[i].StartOffset)
                {
                    isWhile = true;
                }
                else if (FindBreakTarget(endToken.TargetOffset) == null)
                {
                    int elseEnd = FindByTargetOffset(endToken.TargetOffset, ifEnd + 1);
                    if (elseEnd >= 0)
                    {
                        ifEndOffset = this[ifEnd - 1].StartOffset;
                        elseStatement = new ElseStatement(this[ifEnd].StartOffset,
                                                          this[ifEnd - 1].Token,
                                                          GetRange(ifEnd, elseEnd - ifEnd),
                                                          endToken.TargetOffset);
                        ifCount--;
                    }
                }
            }
            CompositeStatement statement = isWhile
                ? (CompositeStatement) new WhileStatement(this[i].StartOffset, jumpToken.Condition, GetRange(i + 1, ifCount), ifEndOffset)
                : new IfStatement(this[i].StartOffset, jumpToken.Condition, GetRange(i + 1, ifCount), ifEndOffset);
            ReplaceRange(i, ifEnd - i, statement);
            statement.ProcessChildren();
            if (elseStatement != null)
            {
                ReplaceRange(i + 1, elseStatement.Children, elseStatement);
                elseStatement.Children.CreateControlStatements();
            }
        }

        private void CreateForeachStatement(int startIndex, ForeachToken token, int endIndex)
        {
            int count = endIndex - startIndex - 1;
            var statement = new ForeachStatement(this[startIndex].StartOffset, token, GetRange(startIndex + 1, count),
                                                 token.TargetOffset);
            ReplaceRange(startIndex, endIndex - startIndex, statement);
            statement.ProcessChildren();
        }

        private bool CreateSwitchStatement(int startIndex, SwitchToken token)
        {
            int endIndex = FindSwitchEndIndex(startIndex + 1);
            if (endIndex >= 0)
            {
                int count = endIndex - startIndex - 1;
                var statement = new SwitchStatement(this[startIndex].StartOffset, token.Expr, GetRange(startIndex + 1, count),
                                                    endIndex);
                ReplaceRange(startIndex, endIndex - startIndex, statement);
                statement.ProcessChildren();
                return true;
            }
            return false;
        }

        private int FindSwitchEndIndex(int startIndex)
        {
            for(int i=startIndex; i<Count; i++)
            {
                if ((this [i].Token is CaseToken || this [i].Token is DefaultToken) && this [i-1].Token is UncondJumpToken)
                {
                    var targetOffset = ((UncondJumpToken) this[i - 1].Token).TargetOffset;
                    return _statements.FindIndex(s => s.StartOffset == targetOffset);
                }
                if (this[i].Token is SwitchToken) return -1;
            }
            if (_statements.Count > 0)
            {
                var returnToken = _statements[_statements.Count - 1].Token as ReturnToken;
                if (returnToken != null && returnToken.ReturnValue is ErrorBytecodeToken)
                {
                    return _statements.Count - 1;
                }
            }
            return _statements.Count;
        }

        public void RemoveRedundantReturns()
        {
            if (_statements.Count > 0)
            {
                var returnToken = _statements[_statements.Count - 1].Token as ReturnToken;
                if (returnToken != null && returnToken.ReturnValue is ErrorBytecodeToken)
                {
                    _statements.RemoveRange(_statements.Count-1, 1);
                }
            }

            // can only detect redundant returns if all jumps were converted to if/else
            if (IsIncompleteControlFlow()) return;

            for (int i = 0; i < Count; i++)
            {
                var returnToken = this[i].Token as ReturnToken;
                if (returnToken != null)
                {
                    if (returnToken.ReturnValue.ToString() == "")
                        RemoveRange(i, Count - i);
                    else
                        RemoveRange(i + 1, Count - i - 1);
                    break;
                }
            }
        }

        public bool IsIncompleteControlFlow()
        {
            var token = Find(s => s.Token is JumpToken && !(s is IControlStatement));
            return token != null;
        }

        public void TrimLast(Func<BytecodeToken, bool> condition)
        {
            if (Count > 0 && condition(this [Count-1].Token))
                RemoveRange(Count-1, 1);
        }

        public bool HasErrors()
        {
            return _statements.Exists(s => s.Token is ErrorBytecodeToken);
        }
    }

    class Statement
    {
        private readonly int _startOffset;
        private readonly int _endOffset;
        protected BytecodeToken _token;

        public Statement(int startOffset, BytecodeToken token)
        {
            _startOffset = startOffset;
            _endOffset = -1;
            _token = token;
        }

        public Statement(int startOffset, int endOffset, BytecodeToken token)
        {
            _startOffset = startOffset;
            _endOffset = endOffset;
            _token = token;
        }

        public BytecodeToken Token
        {
            get { return _token; }
        }

        public int StartOffset
        {
            get { return _startOffset; }
        }

        internal Statement Parent { get; set; }

        public virtual void Print(TextBuilder result, LabelTableToken labelTable, bool showStartOffset)
        {
            if (_token.ToString() == "") return;
            PrintLabel(labelTable, result);
            result.Indent();
            if (showStartOffset)
            {
                result.Append("/* ").Append(_startOffset).Append(" */ ");
            }
            result.Append(_token.ToString(), _startOffset, _endOffset).Append(";");
            result.Append("\n");
        }

        protected void PrintLabel(LabelTableToken labelTable, TextBuilder result)
        {
            if (labelTable != null)
            {
                var label = labelTable.GetLabel(_startOffset);
                if (label != null)
                {
                    result.Append(label).Append(":").NewLine();
                }
            }
        }
    }

    internal interface IControlStatement
    {
    }

    internal abstract class CompositeStatement : Statement
    {
        private readonly StatementList _children;
        private readonly int _endOffset;

        protected CompositeStatement(int startOffset, BytecodeToken token, StatementList children, int endOffset)
            : base(startOffset, token)
        {
            _children = children;
            _children.Parent = this;
            _endOffset = endOffset;
        }

        public override void Print(TextBuilder result, LabelTableToken labelTable, bool showStartOffsets)
        {
            PrintLabel(labelTable, result);
            result.Indent();
            PrintHead(result);
            result.Append("\n");
            result.Indent().Append("{\n");
            PrintChildren(result, labelTable, showStartOffsets);
            result.Indent().Append("}");
            //            result.Append("   // ").Append(_endOffset.ToString());
            result.NewLine();
        }

        protected virtual void PrintChildren(TextBuilder result, LabelTableToken labelTable, bool showStartOffsets)
        {
            _children.Print(result, labelTable, showStartOffsets);
        }

        public virtual void ProcessChildren()
        {
            Children.CreateControlStatements();
        }

        protected abstract void PrintHead(TextBuilder result);

        public StatementList Children { get { return _children; } }
        internal int EndOffset { get { return _endOffset; } }
    }

    internal class IfStatement : CompositeStatement, IControlStatement
    {
        public IfStatement(int startOffset, BytecodeToken token, StatementList children, int endOffset)
            : base(startOffset, token, children, endOffset)
        {
        }

        protected override void PrintHead(TextBuilder result)
        {
            result.Append("if (").Append(_token.ToString()).Append(")");
        }
    }

    internal class ElseStatement : CompositeStatement, IControlStatement
    {
        public ElseStatement(int startOffset, BytecodeToken token, StatementList children, int endOffset)
            : base(startOffset, token, children, endOffset)
        {
        }

        protected override void PrintHead(TextBuilder result)
        {
            result.Append("else");
        }
    }

    internal class WhileStatement : CompositeStatement, IControlStatement
    {
        public WhileStatement(int startOffset, BytecodeToken token, StatementList children, int endOffset)
            : base(startOffset, token, children, endOffset)
        {
        }

        protected override void PrintHead(TextBuilder result)
        {
            result.Append("while (").Append(_token.ToString()).Append(")");
        }

        public override void ProcessChildren()
        {
            Children.CreateControlStatements();
            Children.TrimLast(t => t is JumpToken);
        }
    }

    internal class ForeachStatement : CompositeStatement, IControlStatement
    {
        public ForeachStatement(int startOffset, BytecodeToken token, StatementList children, int endOffset)
            : base(startOffset, token, children, endOffset)
        {
        }

        protected override void PrintHead(TextBuilder result)
        {
            var foreachToken = (ForeachToken)_token;
            result.Append("foreach ").Append(foreachToken.Expr.ToString());
            if (foreachToken.IteratorExpr != null)
            {
                result.Append("(").Append(foreachToken.IteratorExpr.ToString()).Append(")");
            }
        }

        public override void ProcessChildren()
        {
            Children.CreateControlStatements();
            Children.TrimLast(t => t is IteratorPopToken);
            Children.TrimLast(t => t is IteratorNextToken);

            RemoveRedundantPop(Children);
        }

        private static void RemoveRedundantPop(StatementList children)
        {
            for(int i=0; i<children.Count; i++)
            {
                if (children [i] is CompositeStatement)
                {
                    RemoveRedundantPop(((CompositeStatement) children [i]).Children);
                }
                else if (i < children.Count-1 && children [i].Token is IteratorPopToken && children [i+1].Token is ReturnToken)
                {
                    children.RemoveRange(i, 1);
                }
            }
        }
    }

    internal class SwitchStatement : CompositeStatement, IControlStatement
    {
        public SwitchStatement(int startOffset, BytecodeToken token, StatementList children, int endOffset)
            : base(startOffset, token, children, endOffset)
        {
        }

        protected override void PrintHead(TextBuilder result)
        {
            result.Append("switch (").Append(_token.ToString()).Append(")");
        }

        protected override void PrintChildren(TextBuilder result, LabelTableToken labelTable, bool showStartOffsets)
        {
            result.PushIndent();
            bool indented = false;
            for (int i = 0; i < Children.Count; i++)
            {
                var stmt = Children[i];
                if (stmt.Token is CaseToken || stmt.Token is DefaultToken)
                {
                    if (indented)
                    {
                        result.PopIndent();
                        indented = false;
                    }
                    result.Indent().Append(stmt.Token.ToString()).Append(":").NewLine();
                }
                else
                {
                    if (!indented)
                    {
                        indented = true;
                        result.PushIndent();
                    }
                    stmt.Print(result, labelTable, showStartOffsets);
                }
            }
            if (indented) result.PopIndent();
            result.PopIndent();
        }

        public override void ProcessChildren()
        {
            base.ProcessChildren();
            ReplaceJumpsWithBreaks();
            Children.TrimLast(s => s is DefaultToken);
        }

        private void ReplaceJumpsWithBreaks()
        {
            for(int i=0; i<Children.Count; i++)
            {
                var jumpToken = Children[i].Token as UncondJumpToken;
                if (jumpToken != null && jumpToken.TargetOffset == EndOffset)
                {
                    if (i > 0 && Children [i-1].Token is ReturnToken)
                    {
                        Children.RemoveRange(i, 1);
                    }
                    else
                    {
                        Children.ReplaceRange(i, 1, new BreakContinueStatement(Children[i].StartOffset, Children[i].Token, "break"));
                    }
                }
            }
        }
    }

    internal class BreakContinueStatement : Statement, IControlStatement
    {
        private readonly string _text;

        public BreakContinueStatement(int startOffset, BytecodeToken token, string text)
            : base(startOffset, token)
        {
            _text = text;
        }

        public override void Print(TextBuilder result, LabelTableToken labelTable, bool offset)
        {
            result.Indent().Append(_text).Append(";").NewLine();
        }
    }
}