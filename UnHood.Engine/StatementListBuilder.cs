using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnHood.Engine
{
    public class StatementListBuilder
    {
        private readonly StatementList _list = new StatementList();

        private StatementListBuilder AddStatement(int offset, BytecodeToken token)
        {
            _list.Add(new Statement(offset, token));
            return this;
        }

        public StatementListBuilder Add(int offset, string text)
        {
            return AddStatement(offset, new BytecodeToken(text));
        }

        public StatementListBuilder AddForeach(int offset, int targetOffset, string text)
        {
            return AddStatement(offset, new ForeachToken(targetOffset, new BytecodeToken(text)));
        }

        public StatementListBuilder AddIteratorNext(int offset)
        {
            return AddStatement(offset, new IteratorNextToken());
        }

        public StatementListBuilder AddIteratorPop(int offset)
        {
            return AddStatement(offset, new IteratorPopToken());
        }

        public StatementListBuilder AddReturn(int offset)
        {
            _list.Add(new Statement(offset, new ReturnToken(new BytecodeToken(""))));
            return this;
        }

        public StatementListBuilder AddReturn(int offset, string returnValue)
        {
            return AddStatement(offset, new ReturnToken(new BytecodeToken(returnValue)));
        }

        public StatementListBuilder AddErrorReturn(int offset, string returnValue)
        {
            return AddStatement(offset, new ReturnToken(new ErrorBytecodeToken(returnValue, -1, new byte[0])));
        }

        public StatementListBuilder AddJumpIfNot(int offset, int targetOffset, string cond)
        {
            _list.Add(new Statement(offset, new JumpIfNotToken(targetOffset, new BytecodeToken(cond))));
            return this;
        }

        public StatementListBuilder AddJump(int offset, int targetOffset)
        {
            _list.Add(new Statement(offset, new UncondJumpToken(targetOffset)));
            return this;
        }

        public StatementListBuilder AddSwitch(int offset, string text)
        {
            _list.Add(new Statement(offset, new SwitchToken(text, new BytecodeToken(text))));
            return this;
        }

        public StatementListBuilder AddCase(int offset, string text)
        {
            return AddStatement(offset, new CaseToken(text));
        }

        public StatementListBuilder AddDefaultCase(int offset)
        {
            return AddStatement(offset, new DefaultToken());
        }

        public StatementList List
        {
            get { return _list; }
        }
    }
}
