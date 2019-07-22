using System;
using System.Collections.Generic;

namespace CsEarley
{
    public class Parser
    {
        public class Item
        {
            public string Nonterm { get; }
            public IList<string> Rule { get; }
            public int DotPos { get; }
            private string _tos;
            public Item(string nonterm, IList<string> rule, int dotPos = 0)
            {
                Nonterm = nonterm;
                Rule = rule;
                DotPos = dotPos;

                IList<string> tmp = new List<string>(rule);
                tmp.Insert(dotPos, ".");
                _tos = String.Join(" ", tmp);
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
                return _tos;
            }
        }

        public class TreeNode
        {
            public string Content { get; }
            public IList<TreeNode> Children { get; }

            public TreeNode(string type, IList<TreeNode> children)
            {
                this.Content = type;
                this.Children = children;
            }

            public TreeNode(string content)
            {
                this.Content = content;
            }
        }

        public Grammar Grammar { get; }

        public Parser(Grammar g)
        {
            this.Grammar = g;
        }
        
    }
}