using System;
using System.Collections.Generic;
using CsEarley;
using NUnit.Framework;

namespace CsEarleyTests
{
    public class GrammarTests
    {
        [TestCase(
            "S -> a")
        ]
        [TestCase(
            " S -> a ")
        ]
        [TestCase(
            "S->a")
        ]
        [TestCase(
            "S -> E",
            "S -> #",
            "E -> E + T | T",
            "T -> T x F | F",
            "F -> num | id",
            "F -> ( E )"
        )]
        [Test, Category("Input")]
        public void InputSuccessTest(params string[] input)
        {
            // ReSharper disable once ObjectCreationAsStatement
            Assert.DoesNotThrow(() => new Grammar(input));
        }

        [TestCase()]
        [TestCase(
            "S -> $"
        )]
        [TestCase(
            "S -> a |"
        )]
        [TestCase(
            "S -> # a"
        )]
        [TestCase(
            "S ->->"
        )]
        [TestCase(
            "S ->"
        )]
        [TestCase(
            " -> a"
        )]
        [TestCase(
            "S"
        )]
        [Test, Category("Input")]
        public void InputFailureTest(params string[] input)
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
                    {"S", new HashSet<string> {"a", "b", "c"}},
                    {"A", new HashSet<string> {"a", "#"}},
                    {"B", new HashSet<string> {"a", "b", "#"}},
                    {"C", new HashSet<string> {"c", "a", "b"}}
                },
                new[]
                {
                    "S -> A B C",
                    "A -> # | a",
                    "B -> A A | b",
                    "C -> c B | S"
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
    }
}