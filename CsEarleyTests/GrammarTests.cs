using System;
using System.Collections.Generic;
using CsEarley;
using NUnit.Framework;

namespace CsEarleyTests
{
    public class GrammarTests
    {
        private static object[] _inputSuccessTestCases =
        {
            new object[]
            {
                new[]
                {
                    "S -> a"
                }
            },
            new object[]
            {
                new[]
                {
                    " S -> a "
                }
            },
            new object[]
            {
                new[]
                {
                    "S->a"
                }
            },
            new object[]
            {
                new[]
                {
                    "S -> E",
                    "S -> #",
                    "E -> E + T | T",
                    "T -> T x F | F",
                    "F -> num | id",
                    "F -> ( E )"
                }
            }
        };

        [TestCaseSource(nameof(_inputSuccessTestCases))]
        [Test, Category("Input")]
        public void InputSuccessTest(string[] input)
        {
            // ReSharper disable once ObjectCreationAsStatement
            Assert.DoesNotThrow(() => new Grammar(input));
        }

        private static object[] _inputFailureTestCases =
        {
            new object[]
            {
                new string[] { }
            },
            new object[]
            {
                new[]
                {
                    "S -> $"
                }
            },
            new object[]
            {
                new[]
                {
                    "S -> a |"
                }
            },
            new object[]
            {
                new[]
                {
                    "S -> # a"
                }
            },
            new object[]
            {
                new[]
                {
                    "S ->->"
                }
            },
            new object[]
            {
                new[]
                {
                    "S ->"
                }
            },
            new object[]
            {
                new[]
                {
                    " -> a"
                }
            },
            new object[]
            {
                new[]
                {
                    "S"
                }
            }
        };
        [TestCaseSource(nameof(_inputFailureTestCases))]
        [Test, Category("Input")]
        public void InputFailureTest(string[] input)
        {
            // ReSharper disable once ObjectCreationAsStatement
            Assert.Throws<ArgumentException>(() => new Grammar(input));
        }

        private static object[] _firstSetTestCases =
        {
            new object[]
            {
                new Dictionary<string, ISet<string>>
                {
                    {"S", new HashSet<string> {"a", "b", "c", "s"}},
                    {"A", new HashSet<string> {"a", "#"}},
                    {"B", new HashSet<string> {"a", "b", "#"}},
                    {"C", new HashSet<string> {"c"}}
                },
                new[]
                {
                    "S -> A B C | s",
                    "A -> # | a",
                    "B -> A A | b",
                    "C -> C B | c S d"
                }
            },
            new object[]
            {
                new Dictionary<string, ISet<string>>
                {
                    {"S", new HashSet<string> {"b", "c", "#"}},
                    {"A", new HashSet<string> {"#", "b", "c"}},
                    {"B", new HashSet<string> {"b", "#", "c"}},
                    {"C", new HashSet<string> {"c", "b", "#"}}
                },
                new[]
                {
                    "S -> A B C",
                    "A -> S | #",
                    "B -> b | A",
                    "C -> c | B"
                }
            },
            new object[]
            {
                new Dictionary<string, ISet<string>>
                {
                    {"S", new HashSet<string> {"a", "b", "s"}},
                    {"A", new HashSet<string> {"a", "b"}},
                    {"B", new HashSet<string> {"b", "#"}}
                },
                new[]
                {
                    "S -> A B | s",
                    "A -> B A | a",
                    "B -> b S A | #"
                }
            },
            new object[]
            {
                new Dictionary<string, ISet<string>>
                {
                    {"S", new HashSet<string> {"a"}},
                    {"A", new HashSet<string> {"a"}},
                    {"B", new HashSet<string> {"b", "#"}}
                },
                new[]
                {
                    "S -> A B",
                    "A -> a A | a",
                    "B -> b B | #"
                }
            }
        };

        [TestCaseSource(nameof(_firstSetTestCases))]
        [Test, Category("First Follows")]
        public void FirstSetTest(IDictionary<string, ISet<string>> firstSets, string[] grammar)
        {
            var fs = new Grammar(grammar).FirstSets;
            CollectionAssert.AreEquivalent(firstSets.Keys, fs.Keys);
            foreach (var key in firstSets.Keys)
            {
                CollectionAssert.AreEquivalent(fs[key], firstSets[key]);
            }
        }

        private static object[] _followSetTestCases =
        {
            new object[]
            {
                new Dictionary<string, ISet<string>>
                {
                    {"S", new HashSet<string> {"$", "d"}},
                    {"A", new HashSet<string> {"a", "c", "b", "$", "d"}},
                    {"B", new HashSet<string> {"c", "a", "b", "$", "d"}},
                    {"C", new HashSet<string> {"$", "b", "a", "d"}}
                },
                new[]
                {
                    "S -> A B C | s",
                    "A -> # | a",
                    "B -> A A | b",
                    "C -> C B | c S d"
                }
            },
            new object[]
            {
                new Dictionary<string, ISet<string>>
                {
                    {"S", new HashSet<string> {"$", "b", "c"}},
                    {"A", new HashSet<string> {"b", "c", "$"}},
                    {"B", new HashSet<string> {"c", "b", "$"}},
                    {"C", new HashSet<string> {"$", "b", "c"}}
                },
                new[]
                {
                    "S -> A B C",
                    "A -> S | #",
                    "B -> b | A",
                    "C -> c | B"
                }
            },
            new object[]
            {
                new Dictionary<string, ISet<string>>
                {
                    {"S", new HashSet<string> {"$", "a", "b"}},
                    {"A", new HashSet<string> {"$", "b", "a"}},
                    {"B", new HashSet<string> {"$", "a", "b"}}
                },
                new[]
                {
                    "S -> A B | s",
                    "A -> B A | a",
                    "B -> b S A | #"
                }
            },
            new object[]
            {
                new Dictionary<string, ISet<string>>
                {
                    {"S", new HashSet<string> {"$"}},
                    {"A", new HashSet<string> {"b", "$"}},
                    {"B", new HashSet<string> {"$"}}
                },
                new[]
                {
                    "S -> A B",
                    "A -> a A | a",
                    "B -> b B | #"
                }
            }
        };

        [TestCaseSource(nameof(_followSetTestCases))]
        [Test, Category("First Follows")]
        public void FollowSetTest(IDictionary<string, ISet<string>> followSets, string[] grammar)
        {
            var fs = new Grammar(grammar).FollowSets;
            CollectionAssert.AreEquivalent(followSets.Keys, fs.Keys);
            foreach (var key in followSets.Keys)
            {
                CollectionAssert.AreEquivalent(fs[key], followSets[key]);
            }
        }
    }
}