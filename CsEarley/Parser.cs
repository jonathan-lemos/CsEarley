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
            public Item Item { get; }
            private readonly IList<TreeNode> _children;
            public IEnumerable<TreeNode> Children => _children;

            public TreeNode(Item item, IList<TreeNode> children = null)
            {
                Item = item;
                _children = children ?? new List<TreeNode>();
            }
        }

        private class EarleyItem
        {
            public Item Item { get; }
            public int Index { get; }
            public EarleyItem Prev { get; }

            public EarleyItem(Item item, int index, EarleyItem prev)
            {
                Item = item;
                Index = index;
                Prev = prev;
            }

            public void Deconstruct(out Item item, out int index)
            {
                item = Item;
                index = Index;
            }

            protected bool Equals(EarleyItem other)
            {
                return Equals(Item, other.Item) && Index == other.Index;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((EarleyItem) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Item != null ? Item.GetHashCode() : 0) * 397) ^ Index;
                }
            }

            public override string ToString()
            {
                return $"({Item}, {Index})";
            }
        }

        public Grammar Grammar { get; }

        public Parser(Grammar g)
        {
            this.Grammar = g;
        }

        public TreeNode Parse(IEnumerable<string> tokens)
        {
            var words = new List<string>(tokens);
            var newStart = Grammar.Start + "'";
            var table = new List<ISet<EarleyItem>>();
            for (var i = 0; i < words.Count + 1; ++i)
            {
                table.Add(new OrderedSet<EarleyItem>());
            }

            var startRule = new Item(newStart, new List<string> {Grammar.Start});
            var endRule = startRule.Advanced();

            table[0].Add(new EarleyItem(startRule, 0, null));

            for (var i = 0; i < table.Count; ++i)
            {
                var entries = new Queue<EarleyItem>(table[i]);
                while (entries.Count > 0)
                {
                    var earleyItem = entries.Dequeue();
                    var (item, index) = earleyItem;
                    if (!item.IsReduce())
                    {
                        if (Grammar.Nonterms.Contains(item.Current))
                        {
                            foreach (var prod in Grammar[item.Current])
                            {
                                var newItem = new EarleyItem(new Item(item.Current, prod), i, earleyItem);
                                if (!table[i].Contains(newItem))
                                {
                                    entries.Enqueue(newItem);
                                    table[i].Add(newItem);
                                }
                            }
                        }
                        else
                        {
                            if (i < words.Count && item.Current == words[i])
                            {
                                table[i + 1].Add(new EarleyItem(item.Advanced(), index, earleyItem));
                            }
                        }
                    }
                    else
                    {
                        foreach (var (tableItem, tableIndex) in table[index])
                        {
                            if (!tableItem.IsReduce() && tableItem.Current == item.Nonterm)
                            {
                                var newItem = new EarleyItem(tableItem.Advanced(), tableIndex, earleyItem);
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

            var finalRule = table.Last().First(x => x.Item.Equals(endRule));
            if (finalRule == null)
            {
                return null;
            }


            var setIndex = table.Count - 1;

            TreeNode CompleteRule(EarleyItem earleyItem)
            {
                var children = new List<TreeNode>();
                var (item, _) = earleyItem;
                var itemIndex = item.DotPos;
                while (setIndex >= 0 && itemIndex > 0)
                {
                    var prevSymbol = item.Rule[itemIndex - 1];

                    if (Grammar.Terms.Contains(prevSymbol))
                    {
                        setIndex--;
                        var target = table[setIndex].First(x => !x.Item.IsReduce() && x.Item.Current == prevSymbol);
                        children.Add(new TreeNode(target.Item));
                    }
                    else
                    {
                        var target = table[setIndex].First(x => x.Item.IsReduce() && x.Item.Nonterm == prevSymbol);
                        children.Add(CompleteRule(target));
                    }

                    itemIndex--;
                }

                if (setIndex < 0)
                {
                    throw new InvalidOperationException("This is a bug. Ran out of EarleySets to build the tree with.");
                }

                children.Reverse();
                return new TreeNode(item, children);
            }

            return CompleteRule(finalRule);
        }
    }
}