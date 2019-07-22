using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CsEarley
{
    public class Grammar : IEnumerable<KeyValuePair<string, IList<string>>>
    {
        private readonly string _start;
        private readonly IDictionary<string, ISet<IList<string>>> _rules;
        private readonly ISet<string> _terms;
        private readonly ISet<string> _nonterms;
        private readonly ISet<string> _epsilonProducers;
        private readonly IList<string> _nontermsOrdered;
        public string start => _start;

        public ISet<string> Nonterms => _nonterms;

        public ISet<string> Terms => _terms;

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
            var ret = new Dictionary<string, ISet<string>>();
            var epsilon = new HashSet<string> {"#"};
            
            // Initialize each nonterm to be an empty first set
            foreach (var nt in _nontermsOrdered)
            {
                ret.Add(nt, new HashSet<string>());
            }

            // The first set of a token is always that token (except epsilon).
            foreach (var t in _terms)
            {
                ret.Add(t, new HashSet<string>{t});
            }
            
            while (true)
            {
                // We run the following until there's nothing left to update.
                var updated = false;
                // For each production
                foreach (var (nt, prod) in this)
                {
                    // For each token until we reach a non-epsilon-producing token
                    var hit = false;
                    foreach (var token in prod)
                    {
                        var tempLen = ret[nt].Count;
                        // Add the first set of that token to the first set of  
                        ret[nt].UnionWith(ret[token].Except(epsilon));
                        updated |= ret[nt].Count != tempLen;
                        
                        // If this token cannot produce epsilon, then break
                        if (!_epsilonProducers.Contains(token))
                        {
                            hit = true;
                            break;
                        }                
                        
                    }
                }
            }
        }

        public Grammar(IEnumerable<string> rules)
        {
            _rules = new Dictionary<string, ISet<IList<string>>>();
            _nontermsOrdered = new List<string>();
            _nonterms = new HashSet<string>();
            
            foreach (var rule in rules)
            {
                var spl = rule.Split("->");
                if (spl.Length == 0)
                {
                    throw new ArgumentException($"Each rule needs to have a '->'. {rule} does not.");
                }

                if (spl.Length > 2)
                {
                    throw new ArgumentException($"Each rule can only have one '->'. {rule} does not.");
                }

                var nt = spl[0];
                if (_start == null)
                {
                    _start = nt;
                }

                if (!_nonterms.Contains(nt))
                {
                    _nontermsOrdered.Add(nt);
                }
                _nonterms.Add(nt);
                
                var rhs = Regex.Split(spl[1], "\\s+");
                if (!_rules.ContainsKey(nt))
                {
                    _rules[nt] = new HashSet<IList<string>>();
                }
                _rules[nt].Add(rhs.ToList());
            }

            _terms = this.SelectMany(x => x.Value).ToHashSet().Where(x => !_nonterms.Contains(x)).ToHashSet();
            _epsilonProducers = ComputeEpsilonProducers();
        }

        public IEnumerable<IList<string>> this[string s] => _rules[s];

        public IEnumerator<KeyValuePair<string, IList<string>>> GetEnumerator()
        {
            return (_nontermsOrdered.SelectMany(nt => this[nt],
                (nt, prod) => new KeyValuePair<string, IList<string>>(nt, prod))).GetEnumerator();
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