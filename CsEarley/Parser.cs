using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using CsEarley.Functional;

namespace CsEarley
{
    /// <summary>
    /// Parses tokens according to a <see cref="Grammar"/>.
    /// </summary>
    public class Parser
    {
        /// <summary>
        /// Represents an LR(0) Item.
        /// </summary>
        /// These follow the format '<c>Nonterm -> Symbol1 . Symbol2 Symbol3</c>'.
        /// Everything to the left of the dot has been read; everything to the right needs to be read. 
        public class Item : IEquatable<Item>
        {
            /// <summary>
            /// The Nonterminal this <see cref="Item"/> produces.
            /// </summary>
            public readonly string Nonterm;

            /// <summary>
            /// The tokens this rule needs to match.
            /// </summary>
            public readonly IReadOnlyList<string> Rule;

            /// <summary>
            /// The position of the dot within this rule.
            /// </summary>
            public readonly int DotPos;

            /// <summary>
            /// A string representation of this <see cref="Item"/>.
            /// </summary>
            private readonly string _string;

            /// <summary>
            /// Computes a hash code based on the contents of a sequence.
            /// </summary>
            /// This produces the same hash code for the same sequence regardless of the implementing structure.
            /// <param name="sequence">The sequence to hash.</param>
            /// <returns>That sequence's hash code.</returns>
            private static int GetSequenceHashCode<T>(IEnumerable<T> sequence)
            {
                const int seed = 487;
                const int modifier = 31;

                unchecked
                {
                    return sequence.Aggregate(seed, (current, item) =>
                        (current * modifier) + item.GetHashCode());
                }
            }

            /// <summary>
            /// Constructs an <see cref="Item"/>.
            /// </summary>
            /// <param name="nonterm">The Nonterminal this <c>Item</c> produces.</param>
            /// <param name="rule">The Tokens this rule needs to match.</param>
            /// <param name="dotPos">The position of the dot within this rule.</param>
            public Item(string nonterm, IReadOnlyList<string> rule, int dotPos = 0)
            {
                (Nonterm, Rule, DotPos) = (nonterm, rule, dotPos);

                // Insert the dot in the proper location and use it to create the string.
                // We precalculate this string so ToString() is not O(n).
                IList<string> tmp = new List<string>(Rule);
                tmp.Insert(dotPos, ".");
                _string = Nonterm + " -> " + string.Join(" ", tmp);
            }

            /// <summary>
            /// Constructs an <see cref="Item"/>.
            /// </summary>
            /// <param name="nonterm">The Nonterminal this <c>Item</c> produces.</param>
            /// <param name="rule">The Tokens this rule needs to match.</param>
            /// <param name="dotPos">The position of the dot within this rule.</param>
            public Item(string nonterm, IEnumerable<string> rule, int dotPos = 0) : this(nonterm,
                new List<string>(rule), dotPos)
            {
            }

            /// <summary>
            /// Returns the current symbol this <see cref="Item"/> needs to match.
            /// </summary>
            public string Current => Rule[DotPos];

            /// <summary>
            /// Returns true if this item is a "reduce item", meaning it has no more tokens to match (the dot is at the end).
            /// </summary>
            public bool IsReduce()
            {
                return DotPos >= Rule.Count;
            }

            /// <summary>
            /// Returns a new item with the dot moved forward one token.
            /// </summary>
            /// Undefined behavior if you advance an item that <see cref="IsReduce"/>.
            public Item Advanced()
            {
                Debug.Assert(!IsReduce(), "Cannot advance a reduce item.");
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
                return string.Equals(Nonterm, other.Nonterm) && Rule.SequenceEqual(other.Rule) &&
                       DotPos == other.DotPos;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Item) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (Nonterm != null ? Nonterm.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ GetSequenceHashCode(Rule);
                    hashCode = (hashCode * 397) ^ DotPos;
                    return hashCode;
                }
            }
        }

        /// <summary>
        /// A node in a parse tree.
        /// </summary>
        public class TreeNode : IEnumerable<TreeNode>
        {
            /// <summary>
            /// The <see cref="Item"/> that produced this <see cref="TreeNode"/>
            /// </summary>
            public readonly Item Item;

            /// <summary>
            /// If this <see cref="TreeNode"/> was produced from a terminal, this contains the <i>raw</i> token that produced it (not the terminal itself).
            /// Otherwise it contains the nonterminal this <see cref="TreeNode"/> corresponds to.
            /// </summary>
            public readonly string Token;

            /// <summary>
            /// The children of this <see cref="TreeNode"/>.
            /// </summary>
            public readonly IReadOnlyList<TreeNode> Children;

            /// <summary>
            /// Constructs a <see cref="TreeNode"/> (leaf node) from a terminal.
            /// </summary>
            /// <param name="item">The <see cref="Item"/> that produced this <see cref="TreeNode"/></param>
            /// <param name="token">The terminal that produces this <see cref="TreeNode"/></param>
            public TreeNode(Item item, string token)
            {
                Item = item;
                Token = token;
                Children = new List<TreeNode>();
            }

            /// <summary>
            /// Constructs a <see cref="TreeNode"/> (inner node) with a single child.
            /// </summary>
            /// <param name="item">The <see cref="Item"/> that produced this <see cref="TreeNode"/></param>
            /// <param name="child">The child of this <see cref="TreeNode"/></param>
            public TreeNode(Item item, TreeNode child) : this(item, new List<TreeNode> {child})
            {
            }

            /// <summary>
            /// Constructs a <see cref="TreeNode"/> (inner node) with an optional list of children.
            /// </summary>
            /// <param name="item">The <see cref="Item"/> that produced this <see cref="TreeNode"/></param>
            /// <param name="children">A list of children for this <see cref="TreeNode"/>. If this is <c>null</c> or not declared, this <see cref="TreeNode"/> will have no children.</param>
            public TreeNode(Item item, IReadOnlyList<TreeNode> children = null)
            {
                Item = item;
                Children = children ?? new List<TreeNode>();
                Token = item.IsReduce() ? item.Nonterm : item.Current;
            }

            public IEnumerator<TreeNode> GetEnumerator() => Children.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public override string ToString() => Item.ToString();
        }

        /// <summary>
        /// An entry within an Earley set (see <see cref="Parser.Parse"/>).
        /// </summary>
        private class EarleyItem
        {
            /// <summary>
            /// The <see cref="Item"/> that this <see cref="EarleyItem"/> contains.
            /// </summary>
            public readonly Item Item;

            /// <summary>
            /// The index of the set that created the original version of this <see cref="EarleyItem"/>.
            /// </summary>
            /// If this <see cref="EarleyItem"/> contains '<c>S -> A . B C</c>', then this index points to the set that contains '<c>S -> . A B C</c>'.
            public readonly int Origin;

            /// <summary>
            /// The index of the set that contains this <see cref="EarleyItem"/>.
            /// </summary>
            public readonly int Index;

            /// <summary>
            /// The <see cref="EarleyItem"/> that created this one.
            /// </summary>
            /// If this <see cref="Optional{T}"/> is not set, then this is the first <see cref="EarleyItem"/> in the chain.
            public readonly Optional<EarleyItem> Prev;

            /// <summary>
            /// Constructs an <see cref="EarleyItem"/>.
            /// </summary>
            /// <param name="item">The <see cref="Item"/> that this <see cref="EarleyItem"/> contains.</param>
            /// <param name="origin">The index of the set that created the original version of this <see cref="EarleyItem"/>.</param>
            /// <param name="index">The index of the set that contains this <see cref="EarleyItem"/>.</param>
            /// <param name="prev"></param>
            public EarleyItem(Item item, int origin, int index, EarleyItem prev)
            {
                (Item, Origin, Index, Prev) = (item, origin, index, prev ?? new Optional<EarleyItem>());
            }

            /// <summary>
            /// Produces the item and the origin of this EarleyItem.
            /// </summary>
            /// <param name="item"><see cref="EarleyItem.Item"/></param>
            /// <param name="origin"><see cref="EarleyItem.Origin"/></param>
            public void Deconstruct(out Item item, out int origin)
            {
                (item, origin) = (Item, Origin);
            }

            protected bool Equals(EarleyItem other)
            {
                return Equals(Item, other.Item) && Origin == other.Origin;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((EarleyItem) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Item != null ? Item.GetHashCode() : 0) * 397) ^ Origin;
                }
            }

            public override string ToString()
            {
                return $"({Item}, {Origin})";
            }
        }

        /// <summary>
        /// The grammar this <see cref="Parser"/> parses.
        /// </summary>
        public readonly Grammar Grammar;

        /// <summary>
        /// Constructs a <see cref="Parser"/>
        /// </summary>
        /// <param name="g">The grammar this <see cref="Parser"/> should parse for.</param>
        public Parser(Grammar g)
        {
            Grammar = g;
        }

        public class LexException : Exception
        {
            public readonly IReadOnlyList<(string Token, string Raw)> Tokens;

            public LexException(string msg, IEnumerable<(string Token, string Raw)> tokens) : base(msg)
            {
                Tokens = new List<(string Token, string Raw)>(tokens);
            }
        }

        /// <summary>
        /// Lexes a raw input string, returning a list of tokens.
        /// </summary>
        /// Tokens are always separated by any whitespace, meaning <c>"A B"</c> would match the <c>"A"</c> and <c>"B"</c> separately.
        /// However, tokens don't need whitespace to be separated, meaning <c>"f(x)"</c> would match the <c>"f"</c>, <c>"("</c>, <c>"x"</c>, and <c>")"</c> are all separate tokens.
        /// The longest token the grammar can accept is always matched, meaning <c>"whileb"</c> would <i>not</i> match the <c>"while"</c> and <c>"b"</c> separately.
        /// Priority is given to the raw tokens in the grammar before any patterns, meaning <c>"while"</c> always matches as <c>"while"</c> and not an identifier even if your identifier regex is <c>"[a-z]+"</c>.
        /// <param name="input">The raw input string to lex.</param>
        /// <param name="patterns">
        /// <para>
        /// Any tokens that should be matched as regular expressions instead of literal strings.
        /// These tokens should not have anchors like <c>^</c> or <c>$</c> unless they need to be matched at the beginning or end of a line only.
        /// </para>
        /// <example>
        /// For example:
        /// <code>
        /// new[] {
        ///     (Token: "number", Pattern: new Regex(@"\d+")),
        ///     (Token: "identifier", Pattern: new Regex(@"\b[a-zA-Z]+\b"))
        /// }
        /// </code>
        /// </example>
        /// Patterns listed first are matched before other patterns.
        /// Any tokens not covered by a pattern are matched literally.
        /// </param>
        /// <returns>
        /// <para>
        /// Success: A list of tuples containing <c>(Token: token within the grammar, Raw: raw text it came from</c>
        /// Failure: A <see cref="LexException"/> containing an error message and the list of tuples matched.
        /// Bad tuples will have a blank string for the Token field.
        /// </para>
        /// For example: a left parentheses might match as <c>(Token: "(", Raw: "(")</c>, while a number might match as <c>(Token: "number", Raw: "69")</c>.
        /// </returns>
        public Try<IList<(string Token, string Raw)>, LexException> Lex(string input,
            IEnumerable<(string Token, Regex Pattern)> patterns = null)
        {
            // split the input on any whitespace so the regex engine doesn't have to scan one huge input over and over again
            var words = Regex.Split(input, @"\s+", RegexOptions.Multiline)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToList();
            // if patterns is null, we want an empty list to avoid a null check below
            var regexes = patterns != null ? patterns.ToList() : new List<(string Token, Regex Pattern)>();
            // get a list of terminals the regexes can produce. only keep the ones our grammar can actually accept
            var regexTerms = regexes
                .Select(x => x.Token)
                .Where(x => Grammar.Terms.Contains(x))
                .ToHashSet();
            // we want a set of terminals that are matched literally (meaning not matched by a regex), so the raw string "number" isn't matched as an actual number
            var rawTerms = Grammar.Terms
                .Where(x => !regexTerms.Contains(x))
                .ToHashSet();
            // finally, we make a complete list of patterns that match even the raw terms, with earlier ones taking priority over later ones
            var finalPatterns = rawTerms
                .Select(x => (Token: x, Pattern: new Regex(Regex.Escape(x))))
                .Concat(regexes)
                .ToList();
            // our return list
            var ret = new List<(string Token, string Raw)>();

            var badIndex = new Optional<(int Index, string Word)>();

            foreach (var (word, index) in words.Select((x, i) => (x, i)))
            {
                // the position within the current line
                var currentPos = 0;
                while (currentPos < word.Length)
                {
                    // Go through each pattern and pull out the longest token.
                    var longest = (Token: "", Raw: "");
                    foreach (var (token, pattern) in finalPatterns)
                    {
                        var match = pattern.Match(word, currentPos);
                        if (match.Success && match.Index == currentPos && match.Length > longest.Token.Length)
                        {
                            longest = (Token: token, Raw: match.Value);
                        }
                    }

                    // If nothing matches
                    if (longest.Token.Length == 0)
                    {
                        // Set the bad index to the first bad token
                        if (!badIndex)
                        {
                            badIndex = (Index: index, Word: word);
                        }

                        // add a blank token with a single (bad) character.
                        longest = (Token: "", Raw: word[currentPos].ToString());
                    }

                    // Finally add the matched token to the list
                    ret.Add(longest);
                    // And move past the token we just matched
                    currentPos += longest.Raw.Length;
                }
            }

            return badIndex.Match<Try<IList<(string Token, string Raw)>, LexException>>(
                x => new LexException($"Bad token starting with '{x.Word}' (index {x.Index}).", ret),
                () => ret
            );
        }

        public Try<IList<(string Token, string Raw)>, LexException> Lex(string input,
            IEnumerable<(string Token, string Pattern)> patterns)
        {
            return Lex(input, patterns.Select(x => (Token: x.Token, Pattern: new Regex(x.Pattern))));
        }

        public Try<IList<(string Token, string Raw)>, LexException> Lex(string input,
            IEnumerable<KeyValuePair<string, Regex>> patterns)
        {
            return Lex(input, patterns.Select(x => (Token: x.Key, Pattern: x.Value)));
        }

        public Try<IList<(string Token, string Raw)>, LexException> Lex(string input,
            IEnumerable<KeyValuePair<string, string>> patterns)
        {
            return Lex(input, patterns.Select(x => (Token: x.Key, Pattern: new Regex(x.Value))));
        }

        /// <summary>
        /// Builds a parse tree out of a series of tokens and the <see cref="Grammar"/> given in the constructor.
        /// </summary>
        /// These tokens can be built out of an input string with <see cref="Parser.Lex"/>.
        /// <param name="tokens">The tokens to parse.</param>
        /// <returns>A Try{TreeNode, ArgumentException} containing either the parse tree, or an exception showing why parsing failed.</returns>
        public Try<TreeNode, ArgumentException> Parse(IEnumerable<(string Token, string Raw)> tokens)
        {
            // This is where the juice goes down, boys
            // The Earley parser is a DYNAMIC PROGRAMMING top-down parser that completes bottom-up
            // Because it does not use recursion, it doesn't hang on left-recursive grammars like other top-down parsers
            //
            // Essentially, the Earley parser goes word-by-word and keeps track of all possible derivations until this point
            // This means that the Earley parser can process even ambiguous grammars, but this one will only return the first parse tree it finds.
            // It will prioritize rules that come first in the grammar, meaning for "S -> A | a; A -> a", the Earley parser will derive "S -> A" first

            // Make a list of tokens because we need the count
            var words = new List<(string Token, string Raw)>(tokens);

            // Create a new start state so it's easier to build the parser
            var newStart = Grammar.Start + "'";

            // Create a table with an empty set for each word, plus one for the initial state.
            var table = new List<OrderedSet<EarleyItem>>();
            for (var i = 0; i < words.Count + 1; ++i)
            {
                table.Add(new OrderedSet<EarleyItem>());
            }

            // Make the start item and the end item. The start item only produces the grammar's actual start
            var startRule = new Item(newStart, new List<string> {Grammar.Start});
            var endRule = startRule.Advanced();

            // C# is shit so we need to define epsilon as a separate constant
            var epsilon = new List<string> {"#"};

            // Initialize the first state with the start rule
            table[0].Add(new EarleyItem(startRule, 0, 0, null));

            // Each entry of the table corresponds a token in the input sequence with one extra for the start rule
            // In essence, each (LR(0)) state in the table represents the possible derivation paths at that point
            for (var i = 0; i < table.Count; ++i)
            {
                // Mutable iterator allows us to add items to the set and still iterate through it
                foreach (var earleyItem in table[i].MutableIterator())
                {
                    var (item, index) = earleyItem;
                    // If this is not a reduce item
                    if (!item.IsReduce())
                    {
                        // If this is an epsilon production
                        if (item.Rule.SequenceEqual(epsilon))
                        {
                            // Add an item that completes on epsilon
                            var newItem = new EarleyItem(item.Advanced(), i, i, earleyItem);
                            table[i].Add(newItem);
                        }
                        // Otherwise if the next symbol is a Nonterminal
                        else if (Grammar.Nonterms.Contains(item.Current))
                        {
                            // For each production that nonterminal can produce
                            foreach (var prod in Grammar[item.Current])
                            {
                                // Add a new item for each of those productions (compute the LR(0) closure)
                                var newItem = new EarleyItem(new Item(item.Current, prod), i, i, earleyItem);
                                table[i].Add(newItem);
                            }
                        }
                        // If the next symbol is a Terminal
                        else
                        {
                            // If the current token is equal to this terminal
                            if (i < words.Count && item.Current == words[i].Token)
                            {
                                // Shift on this token and add it to the next set
                                table[i + 1].Add(new EarleyItem(item.Advanced(), index, i, earleyItem));
                            }
                        }
                    }
                    // If this is a reduce item
                    else
                    {
                        // Go back to the original state this item came from and go through each item
                        foreach (var (tableItem, tableIndex) in table[index].MutableIterator())
                        {
                            // If we can shift on that item (meaning the next symbol in that item is equal to this nonterm)
                            if (!tableItem.IsReduce() && tableItem.Current == item.Nonterm)
                            {
                                // Add it to the current state
                                var newItem = new EarleyItem(tableItem.Advanced(), tableIndex, i, earleyItem);
                                table[i].Add(newItem);
                            }
                        }
                    }
                }
            }

            // The last state should have a completed state for our start rule. If it doesn't, then this grammar can't accept the input tokens
            var finalRule = table.Last().FirstOrDefault(x => x.Item.Equals(endRule));
            if (finalRule == null)
            {
                return null;
            }

            // recursive method that builds the tree based on the path the earley parse took
            // this path is assembled from all the "prev" items starting with the final rule
            // the complete path makes up the "rightmost derivation" of the tokens
            // the ref parameter makes sure the current item updates for all recursive calls of the function
            TreeNode CompleteRule(ref EarleyItem earleyItem)
            {
                if (earleyItem.Item.IsReduce())
                {
                    var children = new List<TreeNode>();
                    var oldItem = earleyItem.Item;
                    // for a reduce item, we add a child for each token in that item by recursively calling completeRule
                    // we go backwards since our derivation path goes from the final state back to the start state
                    foreach (var token in earleyItem.Item.Rule.Reverse())
                    {
                        earleyItem = earleyItem.Prev.Value;
                        children.Add(CompleteRule(ref earleyItem));
                    }

                    // since we matched the tokens backwards, we reverse the children back to the right order
                    children.Reverse();
                    // get rid of any null children
                    return new TreeNode(oldItem, children.Where(x => x != null).ToList());
                }

                // if this non-reduce item needs to complete a nonterm
                if (Grammar.Nonterms.Contains(earleyItem.Item.Current))
                {
                    // for a non-reduce item, if this is the last item we just return null since it's not important
                    if (!earleyItem.Prev)
                    {
                        return null;
                    }

                    // otherwise, go to the next step in the derivation and recursively completeRule
                    earleyItem = earleyItem.Prev.Value;
                    return CompleteRule(ref earleyItem);
                }

                // if this non-reduce item needs to complete a term, then make a new node containing the raw string that token corresponds to
                return new TreeNode(earleyItem.Item, words[earleyItem.Index].Raw);
            }

            var tree = CompleteRule(ref finalRule);
            // The root node produces our actual start symbol.
            // We want to get rid of that topmost node because all it does is produce the actual start of our tree.
            return tree?.Children.First();
        }
    }
}