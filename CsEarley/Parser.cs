using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CsEarley
{
    public class Parser
    {
        public class Item
        {
            public string Nonterm { get; }
            public IList<string> Rule { get; }
            public int DotPos { get; }
            private string _string;
            public Item(string nonterm, IList<string> rule, int dotPos = 0)
            {
                Nonterm = nonterm;
                Rule = rule;
                DotPos = dotPos;

                IList<string> tmp = new List<string>(rule);
                tmp.Insert(dotPos, ".");
                _string = String.Join(" ", tmp);
            }
            
            public string Current => Rule[DotPos];

            public bool IsReduce()
            { 
                return DotPos >= Rule.Count;  
            } 

            public Item Advanced()
            {
                if (IsReduce())
                {
                    throw new InvalidOperationException("Cannot advance a reduce item.");
                }
                return new Item(Nonterm, Rule, DotPos + 1);
            }

            public override string ToString()
            {
                return _string;
            }
        }

        public class TreeNode
        {
            public string Token { get; }
            private readonly IList<TreeNode> _children;
            public IEnumerable<TreeNode> Children => _children;

            public TreeNode(string token, IList<TreeNode> children)
            {
                Token = token;
                _children = children;
            }
        }

        public Grammar Grammar { get; }

        public Parser(Grammar g)
        {
            this.Grammar = g;
        }

        public void Recognize(string s)
        {
            var words = Regex.Split(s, "\\s+");
            var newStart = Grammar.Start + "'";
            var table = new List<ISet<(Item Item, int Index)>>();
            for (int i = 0; i < words.Length + 1; ++i)
            {
                table.Add(new HashSet<(Item Item, int Index)>());
            }
            table[0].Add((new Item(newStart, new List<string> {Grammar.Start}), 0));

            for (var i = 0; i < table.Count; ++i)
            {
                var entries = new Queue<(Item Item, int Index)>(table[i]);
                while (entries.Count > 0)
                {
                    var state = entries.Dequeue();
                    if (!state.Item.IsReduce())
                    {
                        if (Grammar.Nonterms.Contains(state.Item.Current))
                        {
                            foreach (var prod in Grammar[state.Item.Current])
                            {
                                var newItem = (Item: new Item(state.Item.Nonterm, prod), Index: i);
                                entries.Enqueue(newItem);
                                table[i].Add(newItem);
                            }
                        }
                        else
                        {
                            if (state.Item.Current == words[i])
                            {
                                table[i + 1].Add((Item: state.Item.Advanced(), Index: state.Index));
                            }
                        }
                    }
                    else
                    {
                        
                    }
                }
            }
        }

    }
}