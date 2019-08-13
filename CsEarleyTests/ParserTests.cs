using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using CsEarley;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace CsEarleyTests
{
    public class TestContext
    {
        public readonly Grammar Grammar;
        public readonly IReadOnlyList<(string, string)> Patterns;
        public readonly IReadOnlyList<(string Input, IReadOnlyList<(string Token, string Raw)> Tokens)> TestCases;

        public TestContext(Grammar g, (string, string)[] patterns,
            IEnumerable<(string, (string, string)[])> testCases)
        {
            (Grammar, Patterns) = (g, new List<(string, string)>(patterns));
            TestCases = testCases.Select(test => (test.Item1, test.Item2)).Select(dummy =>
                ((string Input, IReadOnlyList<(string Token, string Raw)> Tokens)) dummy).ToList();
        }
    }

    public class ParserTests
    {
        private static TestContext[] _passContexts =
        {
            new TestContext(
                new Grammar(new[]
                {
                    "func-list -> func func-list | #",
                    "func -> type id ( ) compound-stmt",
                    "stmt-list -> stmt stmt-list | #",
                    "stmt -> compound-stmt | if-stmt | while-stmt | expr-stmt | ;",
                    "compound-stmt -> { stmt-list }",
                    "if-stmt -> if ( expr ) stmt | if ( expr ) stmt else stmt",
                    "while-stmt -> while ( expr ) stmt",
                    "expr-stmt -> expr ;",
                    "expr -> expr + term | term",
                    "term -> term * factor | factor",
                    "factor -> ( expr ) | num"
                }),
                new[]
                {
                    ("type", @"int|void"),
                    ("num", @"\d+"),
                    ("id", @"[a-z]+")
                },
                new[]
                {
                    (
                        @"
void voidfun( ){
while(  1
) 
    {
        if (2+3)
            if ((  4  +5  )*6)
                7;
            else{
                8;
            }
        }
    }

int main () {
    ((9)) + (((10 + 11) + 12) + 13);
    (14 + 15) * 16;
    17;
    if (18 + 19) 20; else 21;
    if ((22 * 23) + (24 * 25)) {
    26;
    }
}
                     "
                        ,
                        new[]
                        {
                            ("type", "void"),
                            ("id", "voidfun"),
                            ("(", "("),
                            (")", ")"),
                            ("{", "{"),
                            ("while", "while"),
                            ("(", "("),
                            ("num", "1"),
                            (")", ")"),
                            ("{", "{"),
                            ("if", "if"),
                            ("num", "2"),
                            ("+", "+"),
                            ("num", "3"),
                            (")", ")"),
                            ("if", "if"),
                            ("(", "("),
                            ("(", "("),
                            ("num", "4"),
                            ("+", "+"),
                            ("num", "5"),
                            (")", ")"),
                            ("*", "*"),
                            ("num", "6"),
                            (")", ")"),
                            ("num", "7"),
                            (";", ";"),
                            ("else", "else"),
                            ("{", "{"),
                            ("num", "8"),
                            (";", ";"),
                            ("}", "}"),
                            ("}", "}"),
                            ("}", "}"),
                            ("type", "int"),
                            ("id", "main"),
                            ("(", "("),
                            (")", ")"),
                            ("{", "{"),
                            ("(", "("),
                            ("(", "("),
                            ("num", "9"),
                            (")", ")"),
                            (")", ")"),
                            ("+", "+"),
                            ("(", "("),
                            ("(", "("),
                            ("(", "("),
                            ("num", "10"),
                            ("+", "+"),
                            ("num", "11"),
                            (")", ")"),
                            ("num", "12"),
                            (")", ")"),
                            ("+", "+"),
                            ("num", "13"),
                            (")", ")"),
                            (";", ";"),
                            ("(", "("),
                            ("num", "14"),
                            ("+", "+"),
                            ("num", "15"),
                            (")", ")"),
                            ("*", "*"),
                            ("num", "16"),
                            (";", ";"),
                            ("num", "17"),
                            (";", ";"),
                            ("if", "if"),
                            ("(", "("),
                            ("num", "18"),
                            ("+", "+"),
                            ("num", "19"),
                            (")", ")"),
                            ("num", "20"),
                            (";", ";"),
                            ("else", "else"),
                            ("num", "21"),
                            (";", ";"),
                            ("if", "if"),
                            ("(", "("),
                            ("(", "("),
                            ("num", "22"),
                            ("*", "*"),
                            ("num", "23"),
                            ("+", "+"),
                            ("(", "("),
                            ("num", "24"),
                            ("*", "*"),
                            ("num", "25"),
                            (")", ")"),
                            (")", ")"),
                            ("{", "{"),
                            ("num", "26"),
                            (";", ";"),
                            ("}", "}"),
                            ("}", "}"),
                        })
                }
            ),
            new TestContext(
                new Grammar(new[]
                {
                    "S -> A B | #",
                    "A -> A num | num",
                    "B -> abc | id | #",
                }),
                new[]
                {
                    ("num", @"\d+"),
                    ("id", @"[a-z]+")
                },
                new[]
                {
                    ("4", new[] {("num", "4")}),
                    ("4 foo", new[] {("num", "4"), ("id", "foo")}),
                    ("44", new[] {("num", "44")}),
                    ("44 44", new[] {("num", "44"), ("num", "44")}),
                    ("44 3 foo", new[] {("num", "44"), ("num", "3"), ("id", "foo")}),
                    ("44 abc", new[] {("num", "44"), ("abc", "abc")}),
                    ("44 ab", new[] {("num", "44"), ("ab", "ab")}),
                    ("44 abcd", new[] {("num", "44"), ("abcd", "abcd")}),
                    ("", new (string, string)[] { })
                }
            )
        };

        private static TestContext[] _lexFailContexts =
        {
            new TestContext(
                new Grammar(new[]
                {
                    "S -> A B",
                    "A -> A num | num",
                    "B -> abc | id | #",
                }),
                new[]
                {
                    ("num", @"\d+"),
                    ("id", @"[a-z]+")
                },
                new (string, (string, string)[])[]
                {
                    ("4 #", null),
                    ("4 4 @foo", null),
                    ("4e", null),
                }
            )
        };

        private static object[] ContextMap(TestContext[] arr)
        {
            return arr.SelectMany(context => context.TestCases,
                    (context, testCase) => new object[]
                        {context.Grammar, context.Patterns, testCase.Input, testCase.Tokens})
                .ToArray<object>();
        }

        private static object[] _lexPassTestCases = ContextMap(_passContexts);
        private static object[] _lexFailTestCases = ContextMap(_lexFailContexts);
        private static object[] _parsePassTestCases = ContextMap(_passContexts);

        [TestCaseSource(nameof(_lexPassTestCases))]
        [Test, Category("Lexer")]
        public void LexPassTest(Grammar g, IEnumerable<(string, string)> patterns,
            string input, IEnumerable<(string Token, string Raw)> expectedTokens)
        {
            new Parser(g).Lex(input).Match(
                tokens => Assert.AreEqual(expectedTokens, tokens),
                ex => throw ex
            );
        }

        [TestCaseSource(nameof(_lexFailTestCases))]
        [Test, Category("Lexer")]
        public void LexFailTest(Grammar g, IEnumerable<(string, string)> patterns, string input)
        {
            new Parser(g).Lex(input).Match(
                tokens => Assert.Fail("Expected failure but lexed successfully."),
                ex => Assert.Pass()
            );
        }

        private IEnumerable<(string Token, string Raw)> GetTokenSequence(Parser.TreeNode root)
        {
            if (root.Children.Count == 0)
            {
                return new List<(string Token, string Raw)> {(Token: root.Item.Current, Raw: root.Token)};
            }
            else
            {
                return root.Children.Aggregate<Parser.TreeNode, IEnumerable<(string Token, string Raw)>>(
                    new List<(string Token, string Raw)>(),
                    (a, c) => a.Concat(GetTokenSequence(c)));
            }
        }

        [TestCaseSource(nameof(_parsePassTestCases))]
        [Test, Category("Parser")]
        public void ParsePassTest(Grammar g, IEnumerable<(string, string)> patterns, string input,
            IEnumerable<(string Token, string Raw)> expectedTokens)
        {
            new Parser(g).Parse(input, patterns).Match(
                tree => Assert.Equals(expectedTokens, GetTokenSequence(tree)),
                ex => throw ex
            );
        }
    }
}