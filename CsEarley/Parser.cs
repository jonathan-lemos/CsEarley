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

            /// <summary>
            /// Returns a new item with the dot moved backward one token.
            /// </summary>
            /// Undefined behavior if you advance an item with <c>DotPos == 0</c>.
            public Item Retarded()
            {
                Debug.Assert(DotPos > 0, "Cannot retard at item at the start.");
                return new Item(Nonterm, Rule, DotPos - 1);
            }

            public string Previous => Rule[DotPos - 1];

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
        public Try<IList<(string Token, string Raw)>, ValuedException<IList<(string Token, string Raw)>>> Lex(string input,
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
                        if (match.Success && match.Index == currentPos && match.Length > longest.Raw.Length)
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

            return badIndex.Match<Try<IList<(string Token, string Raw)>, ValuedException<IList<(string Token, string Raw)>>>>(
                x => new ValuedException<IList<(string Token, string Raw)>>($"Bad token starting with '{x.Word}' (index {x.Index}).", ret),
                () => ret
            );
        }

        public Try<IList<(string Token, string Raw)>, ValuedException<IList<(string Token, string Raw)>>> Lex(string input,
            IEnumerable<(string Token, string Pattern)> patterns)
        {
            return Lex(input, patterns.Select(x => (Token: x.Token, Pattern: new Regex(x.Pattern, RegexOptions.ECMAScript))));
        }

        public Try<IList<(string Token, string Raw)>, ValuedException<IList<(string Token, string Raw)>>> Lex(string input,
            IEnumerable<KeyValuePair<string, Regex>> patterns)
        {
            return Lex(input, patterns.Select(x => (Token: x.Key, Pattern: x.Value)));
        }

        public Try<IList<(string Token, string Raw)>, ValuedException<IList<(string Token, string Raw)>>> Lex(string input,
            IEnumerable<KeyValuePair<string, string>> patterns)
        {
            return Lex(input, patterns.Select(x => (Token: x.Key, Pattern: new Regex(x.Value, RegexOptions.ECMAScript))));
        }

        private class EarleyTable
        {
            public readonly int Count;

            public class EarleyItem
            {
                public readonly Item Item;
                public readonly int Origin;
                public readonly int Index;

                public EarleyItem(Item item, int origin, int index)
                {
                    (Item, Origin, Index) = (item, origin, index);
                }

                public void Deconstruct(out Item item, out int origin)
                {
                    (item, origin) = (Item, Origin);
                }

                protected bool Equals(EarleyItem other)
                {
                    return Item.Equals(other.Item) && Origin == other.Origin;
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
                        return (Item.GetHashCode() * 397) ^ Origin;
                    }
                }

                public override string ToString() => $"({Item}, {Origin}, {Index})";
            }

            public class InnerSet : IEnumerable<EarleyItem>
            {
                private readonly EarleyTable _parent;
                private readonly OrderedSet<EarleyItem> _inner;

                private readonly IDictionary<EarleyItem,
                    Optional<OrderedSet<EarleyItem>>> _prev;

                public InnerSet(EarleyTable parent)
                {
                    _inner = new OrderedSet<EarleyItem>();
                    _prev = new Dictionary<EarleyItem, Optional<OrderedSet<EarleyItem>>>();
                    _parent = parent;
                }

                public IEnumerable<EarleyItem> MutableIterator() => _inner.MutableIterator();

                public IEnumerator<EarleyItem> GetEnumerator() => _inner.GetEnumerator();

                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

                public void Add(Item item, int origin, int index, EarleyItem prev)
                {
                    var newItem = new EarleyItem(item, origin, index);

                    if (_inner.Contains(newItem))
                    {
                        _prev[newItem].Value.Add(prev);
                    }
                    else
                    {
                        _prev[newItem] = prev != null
                            ? new OrderedSet<EarleyItem> {prev}
                            : new Optional<OrderedSet<EarleyItem>>();
                        _inner.Add(newItem);
                    }
                }

                public bool Contains(EarleyItem elem) => _inner.Contains(elem);

                public IDictionary<EarleyItem,
                    Optional<OrderedSet<EarleyItem>>> Prev => _prev;
            }

            private readonly IList<InnerSet> sets;

            public EarleyTable(int capacity, Item startRule)
            {
                sets = new List<InnerSet>(capacity);
                for (var i = 0; i < capacity; ++i)
                {
                    sets.Add(new InnerSet(this));
                }

                sets[0].Add(startRule, 0, 0, null);
                Count = capacity;
            }

            public InnerSet this[int index] => sets[index];

            public InnerSet Last => this[Count - 1];
        }

        /// <summary>
        /// Builds an EarleyTable showing the parsing process of the given tokens through the given grammar.
        /// </summary>
        /// <param name="tokens">A list of tokens optionally produced by <see cref="Parser.Lex"/>.</param>
        /// <returns>A tuple containing the corresponding table and augmented start rule.</returns>
        private (EarleyTable Table, Item StartRule) _buildParseTable(IEnumerable<(string Token, string Raw)> tokens)
        {
            // This is where the juice goes down, boys
            // The Earley parser is a DYNAMIC PROGRAMMING top-down parser that completes bottom-up
            // Because it does not use recursion, it doesn't hang on left-recursive grammars like other top-down parsers
            //
            // Essentially, the Earley parser goes word-by-word and keeps track of all possible derivations until this point
            // This means that the Earley parser can process even ambiguous grammars, but this one will only return the first parse tree it finds.
            // It will prioritize rules that come first in the grammar, meaning for "S -> A | a; A -> a", the Earley parser will derive "S -> A" first

            // Make a list of tokens because we need the count
            var words = tokens.ToList();

            // Create a new start state so it's easier to build the parser
            var newStart = Grammar.Start + "'";
            // Make the start item and the end item. The start item only produces the grammar's actual start
            var startRule = new Item(newStart, new List<string> {Grammar.Start});

            // Create a table with an empty set for each word, plus one for the final reduce state.
            var table = new EarleyTable(words.Count + 1, startRule);

            // C# is shit so we need to define epsilon as a separate constant
            var epsilon = new List<string> {"#"};

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
                            table[i].Add(item.Advanced(), i, i, earleyItem);
                        }
                        // Otherwise if the next symbol is a Nonterminal
                        else if (Grammar.Nonterms.Contains(item.Current))
                        {
                            // For each production that nonterminal can produce
                            foreach (var prod in Grammar[item.Current])
                            {
                                // Add a new item for each of those productions (compute the LR(0) closure)
                                table[i].Add(new Item(item.Current, prod), i, i, earleyItem);
                            }
                        }
                        // If the next symbol is a Terminal
                        else
                        {
                            // If the current token is equal to this terminal
                            if (i < words.Count && item.Current == words[i].Token)
                            {
                                // Shift on this token and add it to the next set
                                table[i + 1].Add(item.Advanced(), index, i + 1, earleyItem);
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
                                table[i].Add(tableItem.Advanced(), tableIndex, i, earleyItem);
                            }
                        }
                    }
                }
            }

            return (table, startRule);
        }

        /// <summary>
        /// Takes the EarleyTable and produces the corresponding rightmost derivation.
        /// </summary>
        /// <param name="input">The EarleyTable and corresponding start rule produced by <see cref="_buildParseTable"/>.</param>
        /// <param name="tokens">The tokens passed to <see cref="_buildParseTable"/>.</param>
        /// <returns>A list corresponding to the rightmost derivation in that table, or an ArgumentException if it could not be made.</returns>
        private Try<IList<EarleyTable.EarleyItem>, ArgumentException> _buildParsePath(
            (EarleyTable Table, Item StartRule) input, IEnumerable<(string Token, string Raw)> tokens)
        {
            var words = tokens.ToList();
            var (table, startRule) = input;
            var endRule = new EarleyTable.EarleyItem(startRule.Advanced(), 0, table.Count - 1);

            // The last state should have a completed state for our start rule. If it doesn't, then this grammar can't accept the input tokens
            if (!table.Last.Contains(endRule))
            {
                return new ArgumentException(
                    "The table does not show a complete derivation (the input string was not accepted).");
            }

            // The completed path goes here
            var path = new List<EarleyTable.EarleyItem>();

            // The raw tokens we still need to match. We use a stack because we're doing a rightmost derivation, so we match tokens backwards.
            var tokensToMatch = new Stack<string>();
            foreach (var word in words)
            {
                tokensToMatch.Push(word.Token);
            }

            // This stack represents the items we need to match
            // Every time we descend into another rule we push. Every time we return from a rule we pop.
            var stack = new Stack<EarleyTable.EarleyItem>();
            stack.Push(endRule);

            while (stack.Count > 0)
            {
                // Take an item off the stack
                var current = stack.Pop();
                // If the index is unknown.
                if (current.Index == -1)
                {
                    // Set it to the index of the last item matched (this is a disgusting bug filled hack please fix)
                    current = new EarleyTable.EarleyItem(current.Item, current.Origin, path.Last().Index);
                }

                // Add that item to our path
                path.Add(current);

                // If this item has no previous element, continue to clear out the remainder of the stack
                if (table[tokensToMatch.Count].Prev.ContainsKey(current) && !table[tokensToMatch.Count].Prev[current])
                {
                    continue;
                }

                // If this item has no previous
                if (current.Item.DotPos == 0)
                {
                    // Pop another item (the one we need to match)
                    var toMatch = stack.Pop();
                    if (toMatch.Item.DotPos > 0)
                    {
                        // Get the previous element that equals the one we need to match
                        // This makes sure the index is correct since the index is not included in comparisons
                        var prospect = table[current.Index].Prev[current].Value.First(x => x.Equals(toMatch));
                        // push it
                        stack.Push(prospect);
                    }
                }
                // If this item has a previous
                else
                {
                    // Get that previous token
                    var toMatchToken = current.Item.Previous;
                    // Store the previous item for later
                    // Index is -1 because we don't know the correct index at the moment
                    var target = new EarleyTable.EarleyItem(current.Item.Retarded(), current.Origin, -1);
                    // If that previous token is the next token we need to match or it is epsilon
                    if ((Grammar.Terms.Contains(toMatchToken) && toMatchToken == tokensToMatch.Peek()) ||
                        toMatchToken == "#")
                    {
                        // Pop the next token if it's not epsilon
                        if (toMatchToken != "#")
                        {
                            tokensToMatch.Pop();
                        }

                        // Get the previous item with the correct index
                        var prospect = table[current.Index].Prev[current].Value.Get(target);
                        // Push it
                        stack.Push(prospect);
                    }
                    // If the previous token is a nonterm
                    else
                    {
                        // Get the first out of the previous set that produces that nonterm
                        var prospect = table[current.Index].Prev[current].Value
                            .First(x => x.Item.Nonterm == toMatchToken);
                        // Push the target item
                        stack.Push(target);
                        // Push that previous item
                        stack.Push(prospect);
                    }
                }
            }

            return path;
        }

        /// <summary>
        /// Builds a parse tree out of a rightmost derivation.
        /// </summary>
        /// <param name="items">The rightmost derivation produced by <see cref="_buildParsePath"/>.</param>
        /// <param name="tokens">The tokens given to <see cref="_buildParsePath"/>.</param>
        /// <returns>The root node of the produced parse tree, or ArgumentException if it could not be produced.</returns>
        private Try<TreeNode, ArgumentException> _buildParseTree(IEnumerable<EarleyTable.EarleyItem> items,
            IEnumerable<(string Token, string Raw)> tokens)
        {
            var words = tokens.ToList();

            using (var enumerator = items.GetEnumerator())
            {
                enumerator.MoveNext();
                var earleyItem = enumerator.Current;

                TreeNode CompleteRule()
                {
                    if (earleyItem == null)
                    {
                        return null;
                    }

                    if (earleyItem.Item.IsReduce())
                    {
                        var children = new List<TreeNode>();
                        var oldItem = earleyItem.Item;
                        // for a reduce item, we add a child for each token in that item by recursively calling completeRule
                        // we go backwards since our derivation path goes from the final state back to the start state
                        foreach (var token in earleyItem.Item.Rule.Reverse())
                        {
                            enumerator.MoveNext();
                            earleyItem = enumerator.Current;
                            children.Add(CompleteRule());
                        }

                        // since we matched the tokens backwards, we reverse the children back to the right order
                        children.Reverse();
                        // get rid of any null children
                        return new TreeNode(oldItem, children.Where(x => x != null).ToList());
                    }

                    // if this non-reduce item needs to complete a nonterm
                    if (Grammar.Nonterms.Contains(earleyItem.Item.Current))
                    {
                        // otherwise, go to the next step in the derivation and recursively completeRule
                        enumerator.MoveNext();
                        earleyItem = enumerator.Current;

                        // for a non-reduce item, if this is the last item we just return null since it's not important
                        if (earleyItem == null)
                        {
                            return null;
                        }

                        return CompleteRule();
                    }

                    // if the current token is epsilon
                    if (earleyItem.Item.Current == "#")
                    {
                        return new TreeNode(earleyItem.Item);
                    }

                    // if this non-reduce item needs to complete a term, then make a new node containing the raw string that token corresponds to
                    return new TreeNode(earleyItem.Item, words[earleyItem.Index].Raw);
                }

                // The top node has our augmented start rule producing the actual start rule
                // We get rid of the top node by returning the first (only) child
                return CompleteRule().Children.First();
            }
        }

        /// <summary>
        /// Builds a parse tree out of a series of tokens and the <see cref="Grammar"/> given in the constructor.
        /// </summary>
        /// These tokens can be built out of an input string with <see cref="Parser.Lex"/>.
        /// <param name="tokens">The tokens to parse.</param>
        /// <returns>A Try{TreeNode, ArgumentException} containing either the parse tree, or an exception showing why parsing failed.</returns>
        public Try<TreeNode, ArgumentException> Parse(IEnumerable<(string Token, string Raw)> tokens)
        {
            var words = tokens.ToList();
            var res = _buildParseTable(words);
            var ret = _buildParsePath(res, words).Match(
                path => _buildParseTree(path, words),
                ex => ex
            );
            return ret;
        }

        public Try<TreeNode, ArgumentException> Parse(string input,
            IEnumerable<(string Token, Regex Pattern)> patterns)
        {
            return Lex(input, patterns).Match(
                Parse,
                ex => new ArgumentException("Failed to lex the input string.", ex)
            );
        }

        public Try<TreeNode, ArgumentException> Parse(string input,
            IEnumerable<(string Token, string Pattern)> patterns)
        {
            return Parse(input, patterns.Select(x => (Token: x.Token, Pattern: new Regex(x.Pattern))));
        }

        public Try<TreeNode, ArgumentException> Parse(string input,
            IEnumerable<KeyValuePair<string, Regex>> patterns)
        {
            return Parse(input, patterns.Select(x => (Token: x.Key, Pattern: x.Value)));
        }

        public Try<TreeNode, ArgumentException> Parse(string input,
            IEnumerable<KeyValuePair<string, string>> patterns)
        {
            return Parse(input, patterns.Select(x => (Token: x.Key, Pattern: new Regex(x.Value))));
        }
    }
}