using System;
using System.Collections.Generic;

namespace CsEarley
{
    public class Parser
    {
        public class Item
        {
            private readonly string _nt;
            private readonly IList<string> _rule;
            private readonly int _dotpos;
            
            public string nt => _nt;
            public IList<string> rule => _rule;

            public Item(string nt, IList<string> rule, int dotpos = 0)
            {
                this._nt = nt;
                this._rule = rule;
                this._dotpos = dotpos;
            }

            public override string ToString()
            {
                return String.Join(" ", _rule);
            }
        }
        
        private readonly Grammar _grammar;
        public Grammar Grammar => _grammar;
        
        public Parser(Grammar g)
        {
            this._grammar = g;
        }
    }
}