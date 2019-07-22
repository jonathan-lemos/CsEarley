using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Text.RegularExpressions;

namespace CsEarley
{
    public class Grammar : IEnumerable<KeyValuePair<string, IList<string>>>
    {
        private readonly IDictionary<string, ISet<IList<string>>> _rules;
        private readonly ISet<string> _terms;
        private readonly ISet<string> _nonterms;
        private readonly ISet<string> _epsilonProducers;
        private readonly IDictionary<string, ISet<string>> _firstSets;
        private readonly IDictionary<string, ISet<string>> _followSets;
        private readonly IList<KeyValuePair<string, IList<string>>> _prods;
        
        public string Start { get; }
        public IEnumerable<string> Nonterms => _nonterms;
        public IEnumerable<string> Terms => _terms;
        public IReadOnlyDictionary<string, ISet<string>> FirstSets => new ReadOnlyDictionary<string, ISet<string>>(_firstSets);
        public IReadOnlyDictionary<string, ISet<string>> FollowSets => new ReadOnlyDictionary<string, ISet<string>>(_followSets);
        public IEnumerable<string> EpsilonProducers => _epsilonProducers;
        public IEnumerable<KeyValuePair<string, IList<string>>> Productions => _prods;
        
        private ISet<string> ComputeEpsilonProducers()
        {
            // "#" is the epsilon token because we can't type the actual epsilon.
            
            var ret = new HashSet<string> ();
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
            
            return ret;
        }

        private IDictionary<string, ISet<string>> ComputeFirstSets()
        {
            var epsilon = new HashSet<string> {"#"};
            
            // Initialize each nonterm to be an empty first set
            var ret = _nonterms.ToDictionary<string, string, ISet<string>>(nt => nt, nt => new HashSet<string>());

            // The first set of a token is always that token
            foreach (var t in _terms)
            {
                ret.Add(t, new HashSet<string>{t});
            }
            
            while (true)
            {
                // We run the following until there's nothing left to update.
                var updated = false;
                
                // For each production in this grammar
                foreach (var (nt, prod) in this)
                {
                    var allEpsilon = false;
                    
                    // Go through each token of the production until we hit one that cannot produce epsilon
                    foreach (var token in prod)
                    {
                        // Save the old length so we can see if nt's first set changes
                        var tempLen = ret[nt].Count;
                        
                        // Add the first set of that token to the first set of this nonterm
                        ret[nt].UnionWith(ret[token].Except(epsilon));
                       
                        // Set updated to true if the number of elements in nt's first set changed
                        updated |= ret[nt].Count != tempLen;
                        
                        // If the current token cannot produce epsilon, then break
                        if (!_epsilonProducers.Contains(token))
                        {
                            allEpsilon = true;
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

            return ret;
        }

        private IDictionary<string, ISet<string>> ComputeFollowSets()
        {
            var epsilon = new HashSet<string> {"#"};
            
            // Create first sets including terminals (which have first sets of only themselves)
            var fs = new Dictionary<string, ISet<string>>(_firstSets);
            foreach (var term in _terms)
            {
                fs.Add(term, new HashSet<string> {term});
            }
            
            // Start off with blank follow sets except for the start symbol, which can always be followed by "$", the end symbol.
            var ret = _nonterms.ToDictionary<string, string, ISet<string>>(nt => nt, nt => new HashSet<string>());
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
                        if (_nonterms.Contains(token))
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
                        if (_epsilonProducers.Contains(token))
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

        public Grammar(IEnumerable<string> rules)
        {
            _rules = new Dictionary<string, ISet<IList<string>>>();
            _prods = new List<KeyValuePair<string, IList<string>>>();
            _nonterms = new HashSet<string>();
           
            // For each input rule
            foreach (var rule in rules)
            {
                // Turn "a -> bcd efg hij" into ["a", "bcd efg hij"]
                // This should have two elements, the nonterm and the right-hand-side
                var spl = rule.Split("->").Select(x => x.Trim()).ToList();
                if (spl.Count == 0)
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
                
                // Add this nonterm to the nonterm set if it doesn't exist yet
                _nonterms.Add(nt);
                
                // Split the right-hand-side into each token on whitespace.
                // "bcd efg hij" -> ["bcd", "efg", "hij"]
                var rhs = Regex.Split(spl[1], "\\s+").ToList();
                
                // Initialize a blank set for this nonterm if it's not currently in our rules dict
                if (!_rules.ContainsKey(nt))
                {
                    _rules[nt] = new HashSet<IList<string>>();
                }
                // Add this production into this nonterm's rules
                _rules[nt].Add(rhs);
                
                // Add this production to our list of productions.
                // This is only used for enumerator purposes because it preserves the original input order.
                _prods.Add(new KeyValuePair<string, IList<string>>(nt, rhs));
            }

            _terms = this.SelectMany(x => x.Value).ToHashSet().Where(x => !_nonterms.Contains(x)).ToHashSet();
            _epsilonProducers = ComputeEpsilonProducers();
            _firstSets = ComputeFirstSets();
            _followSets = ComputeFollowSets();
        }

        public IEnumerable<IList<string>> this[string s] => _rules[s];

        public IEnumerator<KeyValuePair<string, IList<string>>> GetEnumerator()
        {
            return _prods.GetEnumerator();
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