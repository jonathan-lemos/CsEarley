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
        public void InputFailureTest(params string[] input)
        {
            // ReSharper disable once ObjectCreationAsStatement
            Assert.Throws<ArgumentException>(() => new Grammar(input));
        }

        public void FirstSetTest(IDictionary<string, ISet<string>> firstSets, params string[] grammar)
        {
            Assert.AreEqual(firstSets, new Grammar(grammar).FirstSets);
        }
    }
}