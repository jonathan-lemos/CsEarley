using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CsEarley
{
    public class Parser
    {
        public class Item : IEquatable<Item>
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
                _string = Nonterm + " -> " + string.Join(" ", tmp);
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

            public bool Equals(Item other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(Nonterm, other.Nonterm) && Equals(Rule, other.Rule) && DotPos == other.DotPos;
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj) || obj is Item other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (Nonterm != null ? Nonterm.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Rule != null ? Rule.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ DotPos;
                    return hashCode;
                }
            }
        }

        public class TreeNode
        {
            public string Token { get; }
            private readonly IList<TreeNode> _children;
            public IEnumerable<TreeNode> Children => _children;

            public TreeNode(string token, IList<TreeNode> children = null)
            {
                Token = token;
                _children = children ?? new List<TreeNode>();
            }
        }

        public Grammar Grammar { get; }

        public Parser(Grammar g)
        {
            this.Grammar = g;
        }

        public bool Recognize(string s)
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
                    var (item, index) = entries.Dequeue();
                    if (!item.IsReduce())
                    {
                        if (Grammar.Nonterms.Contains(item.Current))
                        {
                            foreach (var prod in Grammar[item.Current])
                            {
                                var newItem = (Item: new Item(item.Current, prod), Index: i);
                                if (!table[i].Contains(newItem))
                                { 
                                    entries.Enqueue(newItem);
                                    table[i].Add(newItem);   
                                }
                            }
                        }
                        else
                        {
                            if (i < words.Length && item.Current == words[i])
                            {
                                table[i + 1].Add((Item: item.Advanced(), Index: index));
                            }
                        }
                    }
                    else
                    {
                        foreach (var (tableItem, tableIndex) in table[index])
                        {
                            if (!tableItem.IsReduce() && tableItem.Current == item.Nonterm)
                            { 
                                var newItem = (Item: tableItem.Advanced(), Index: tableIndex);
                                if (!table[i].Contains(newItem))
                                {
                                    table[i].Add(newItem);
                                    entries.Enqueue(newItem);   
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

    }
}