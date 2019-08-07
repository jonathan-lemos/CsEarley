using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace CsEarley
{
    /// <summary>
    /// Represents a context free grammar (CFG).
    /// </summary>
    public class Grammar : IEnumerable<KeyValuePair<string, IList<string>>>
    {
        private readonly IDictionary<string, ISet<IList<string>>> _rules;
        private readonly IDictionary<string, ISet<string>> _firstSets;
        private readonly IDictionary<string, ISet<string>> _followSets;
        private readonly IList<KeyValuePair<string, IList<string>>> _prods;

        /// <summary>
        /// The start symbol of the grammar.
        /// </summary>
        public readonly string Start;

        /// <summary>
        /// The terminals (characters that appear in strings) in the grammar.
        /// </summary>
        public readonly ImmutableOrderedSet<string> Terms;

        /// <summary>
        /// The nonterminals (characters that produce patterns) in the grammar.
        /// </summary>
        public readonly ImmutableOrderedSet<string> Nonterms;

        /// <summary>
        /// The symbols (terminals + nonterminals) in the grammar.
        /// </summary>
        public readonly ImmutableOrderedSet<string> Symbols;

        /// <summary>
        /// The nonterminals that can produce epsilon (empty string).
        /// </summary>
        /// Epsilon is represented as '#'.
        public readonly ImmutableOrderedSet<string> EpsilonProducers;

        /// <summary>
        /// The terminals that can appear first in a production of each nonterminal.
        /// </summary>
        public IReadOnlyDictionary<string, ISet<string>> FirstSets =>
            new ReadOnlyDictionary<string, ISet<string>>(_firstSets);

        /// <summary>
        /// The terminals that can follow a production of each nonterminal.
        /// </summary>
        /// End of input is represented as '$'.
        public IReadOnlyDictionary<string, ISet<string>> FollowSets =>
            new ReadOnlyDictionary<string, ISet<string>>(_followSets);

        /// <summary>
        /// All of the productions in this grammar.
        /// </summary>
        /// The productions will be listed in the order provided in the constructor.
        /// A production is of the format 'nonterminal -> symbol1 symbol2 symbol3 ...'
        /// The key in the KeyValuePair is the nonterminal.
        /// The IList{string} is the list of symbols that need to be met.
        public IEnumerable<KeyValuePair<string, IList<string>>> Productions => _prods;

        private ImmutableOrderedSet<string> ComputeEpsilonProducers()
        {
            // "#" is the epsilon token because we can't type the actual epsilon.
            var epsilon = new OrderedSet<string> {"#"};

            var ret = new OrderedSet<string> {"#"};
            // While the above set is changing.
            while (true)
            {
                // Save the current length of the above set to see if it changes.
                var tempLength = ret.Count;

                // For each production in the current grammar
                foreach (var (nt, prod) in this)
                {
                    // If all of the tokens in this production can produce epsilon.
                    if (prod.All(token => ret.Contains(token)))
                    {
                        // Then this nonterm can produce epsilon, so add it to the set.
                        ret.Add(nt);
                    }
                }

                // If the return set didn't change this iteration
                if (ret.Count == tempLength)
                {
                    // Then we have found each nonterminal that can produce epsilon, so exit the loop
                    break;
                }
            }

            return new OrderedSet<string>(ret.Except(epsilon));
        }

        private IDictionary<string, ISet<string>> ComputeFirstSets()
        {
            var epsilon = new OrderedSet<string> {"#"};

            // Initialize each nonterm to be an empty first set
            var ret = Nonterms.ToDictionary<string, string, ISet<string>>(nt => nt, nt => new OrderedSet<string>());

            // The first set of a token is always that token
            foreach (var t in Terms)
            {
                ret.Add(t, new OrderedSet<string> {t});
            }

            while (true)
            {
                // We run the following until there's nothing left to update.
                var updated = false;

                // For each production in this grammar
                foreach (var (nt, prod) in this)
                {
                    var allEpsilon = true;

                    // Go through each token of the production until we hit one that cannot produce epsilon
                    foreach (var token in prod)
                    {
                        // Save the old length so we can see if nt's first set changes
                        var tempLen = ret[nt].Count;

                        // If the token is epsilon, add epsilon to our first set.
                        if (token == "#")
                        {
                            ret[nt].Add("#");
                        }
                        // Otherwise, add the first set of that token to the first set of our token minus epsilon.
                        // We subtract epsilon because instead of making a blank string,  we add the first set of the next token.
                        else
                        {
                            ret[nt].UnionWith(ret[token].Except(epsilon));
                        }

                        // Set updated to true if the number of elements in nt's first set changed
                        updated |= ret[nt].Count != tempLen;

                        // If the current token cannot produce epsilon, then break
                        if (!EpsilonProducers.Contains(token))
                        {
                            allEpsilon = false;
                            break;
                        }
                    }

                    // If all of the tokens in the production can produce epsilon
                    if (allEpsilon)
                    {
                        // Then add epsilon to nt's first set.
                        ret[nt].UnionWith(epsilon);
                    }
                }

                // If none of the first sets updated
                if (!updated)
                {
                    // Then we are done, so exit the loop
                    break;
                }
            }

            // Get rid of all terminals in the dictionary. Then return that dictionary
            return ret.Where(x => !Terms.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
        }

        private IDictionary<string, ISet<string>> ComputeFollowSets()
        {
            var epsilon = new OrderedSet<string> {"#"};

            // Create first sets including terminals (which have first sets of only themselves)
            var fs = new Dictionary<string, ISet<string>>(_firstSets);
            foreach (var term in Terms)
            {
                fs.Add(term, new OrderedSet<string> {term});
            }

            // Start off with blank follow sets except for the start symbol, which can always be followed by "$", the end symbol.
            var ret = Nonterms.ToDictionary<string, string, ISet<string>>(nt => nt, nt => new OrderedSet<string>());
            ret[Start].Add("$");

            while (true)
            {
                // We run the following until there is nothing left to update
                var updated = false;

                foreach (var (nt, prod) in this)
                {
                    // currentFollow is what can follow the current symbol
                    // By default this is the follow set of the current nonterminal
                    var currentFollow = ret[nt];

                    // start from the back of this production and go toward the front
                    foreach (var token in prod.Reverse())
                    {
                        // if the current token is a nonterm
                        if (Nonterms.Contains(token))
                        {
                            // then add the running follow set to this token's follow set

                            // Save the old count so we can see if the token's follow set changes
                            var tmp = ret[token].Count;

                            // Add the current follow set to this token's follow set
                            ret[token].UnionWith(currentFollow);

                            // Set updated to true if the set changed
                            updated |= ret[token].Count != tmp;
                        }

                        // if this token can produce epsilon
                        if (EpsilonProducers.Contains(token))
                        {
                            // then add the first set of this token to the running follow set
                            // this is because if this token produces epsilon, then the follow set of the next token needs to be included
                            currentFollow.UnionWith(fs[token].Except(epsilon));
                        }
                        else
                        {
                            // otherwise, simply set the running follow set to the first set of this token
                            currentFollow = fs[token];
                        }
                    }
                }

                // If nothing was updated on this iteration
                if (!updated)
                {
                    // Then exit the loop
                    break;
                }
            }

            return ret;
        }

        /// <summary>
        /// Constructs a <see cref="Grammar"/>.
        /// </summary>
        /// <param name="rules">
        /// A list of rules in the grammar.
        /// </param>
        /// These rules are of the format 'nonterminal -> aaa bbb ccc | ddd eee | fff ...'.
        /// Epsilon is represented as '#'.
        /// The symbols on the right-hand side can be any number of characters, but each symbol needs to be separated by one or more spaces.
        /// A symbol cannot be '->', '#', '|', or '$'.
        /// Alternate productions for a nonterminal can be specified with the vertical bar '|'.
        /// Alternate productions for a nonterminal can also be specified with another string using that same nonterminal on the left.
        /// The first production's nonterminal is treated as the start symbol.
        /// Symbols that do not appear as nonterminals are treated as terminals.
        /// Note that a given string must contain one and only one '->'.
        /// <example>
        /// An example of a basic arithmetic grammar:
        /// <code>
        /// new Grammar(new[] {
        /// "start -> expression | term | factor",
        /// "start -> #",
        /// "expression -> expression + term | term",
        /// "term -> term * factor | factor",
        /// "factor -> number | ( expression )"
        /// });
        /// </code>
        /// Note that the above grammar is ambiguous.
        /// </example>
        /// <exception cref="ArgumentException">A given input was invalid.</exception>
        public Grammar(IEnumerable<string> rules)
        {
            _rules = new Dictionary<string, ISet<IList<string>>>();
            _prods = new List<KeyValuePair<string, IList<string>>>();
            var nonterms = new OrderedSet<string>();

            // For each input rule
            foreach (var rule in rules)
            {
                // Turn "a -> bcd efg hij" into ["a", "bcd efg hij"]
                // This should have two elements, the nonterm and the right-hand-side
                var spl = rule.Split("->").Select(x => x.Trim()).ToList();
                
                if (spl.Count < 2)
                {
                    throw new ArgumentException($"Each rule needs to have a '->'. '{rule}' does not.");
                }
                if (spl.Count > 2)
                {
                    throw new ArgumentException($"Each rule can only have one '->'. '{rule}' does not.");
                }

                // The nonterm is the first part of the list
                var nt = spl[0];
                // The first rule is the start rule
                if (Start == null)
                {
                    Start = nt;
                }

                switch (nt)
                {
                    case "$":
                    case "|":
                        throw new ArgumentException($"'{nt}' is not a valid nonterminal. Seen in rule '{rule}'.");
                    case "":
                        throw new ArgumentException($"Rules cannot have blank nonterminals. Seen in rule '{rule}'.");
                }

                // Add this nonterm to the nonterm set if it doesn't exist yet
                nonterms.Add(nt);

                // First split the right-hand-side on any |
                // "foo bar | qqq | woo woo woo" -> ["foo bar", "qqq", "woo woo woo"]
                var tmp = Regex.Split(spl[1], @"\|").Select(x => x.Trim()).ToList();

                foreach (var prod in tmp)
                {
                    // Split the right-hand-side into each token on whitespace.
                    // "bcd efg hij" -> ["bcd", "efg", "hij"]
                    var rhs = Regex.Split(prod, @"\s+").Select(x => x.Trim()).ToList();

                    // Initialize a blank set for this nonterm if it's not currently in our rules dict
                    if (!_rules.ContainsKey(nt))
                    {
                        _rules[nt] = new OrderedSet<IList<string>>();
                    }

                    if (rhs.Count == 1 && rhs[0] == "")
                    {
                        throw new ArgumentException($"A production cannot be empty. Seen in rule '{rule}'.");
                    }

                    // Make sure if the production contains epsilon, then epsilon is the only token
                    if (rhs.Count != 1 && rhs.Contains("#"))
                    {
                        throw new ArgumentException(
                            $"A production cannot contain epsilon (#) and another symbol. Seen in rule '{rule}'.");
                    }

                    if (rhs.Contains("$"))
                    {
                        throw new ArgumentException($"'$' cannot be used as a symbol. Seen in rule '{rule}'.");
                    }

                    // Add this production into this nonterm's rules
                    _rules[nt].Add(rhs);

                    // Add this production to our list of productions.
                    // This is only used for enumerator purposes because it preserves the original input order.
                    _prods.Add(new KeyValuePair<string, IList<string>>(nt, rhs));
                }
            }

            if (Nonterms.Count == 0)
            {
                throw new ArgumentException("A Grammar needs to have at least one production.");
            }

            Nonterms = nonterms;
            // Get a set of all symbols in the grammar. The ones that are not nonterminals are automatically terminals.
            Terms = new OrderedSet<string>(
                this.SelectMany(x => x.Value).ToHashSet().Where(x => !Nonterms.Contains(x)));
            Symbols = new OrderedSet<string>(Terms.Union(Nonterms));

            EpsilonProducers = ComputeEpsilonProducers();
            _firstSets = ComputeFirstSets();
            _followSets = ComputeFollowSets();
        }

        public IEnumerable<IList<string>> this[string s] => _rules[s];

        public IEnumerator<KeyValuePair<string, IList<string>>> GetEnumerator()
        {
            return Productions.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return string.Join("\n", this.Select(x => x.Key + " -> " + string.Join(" ", x.Value)));
        }
    }
}