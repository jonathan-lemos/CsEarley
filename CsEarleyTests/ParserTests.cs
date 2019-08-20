using System.Collections.Generic;
using System.Linq;
using CsEarley;
using NUnit.Framework;

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
                    ("num", @"\b\d+\b"),
                    ("id", @"\b[a-z]+\b")
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
                            ("(", "("),
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
                            ("+", "+"),
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
                            (")", ")"),
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
                    ("num", @"\b\d+\b"),
                    ("id", @"\b[a-z]+\b")
                },
                new[]
                {
                    ("4", new[] {("num", "4")}),
                    ("4 foo", new[] {("num", "4"), ("id", "foo")}),
                    ("44", new[] {("num", "44")}),
                    ("44 44", new[] {("num", "44"), ("num", "44")}),
                    ("44 3 foo", new[] {("num", "44"), ("num", "3"), ("id", "foo")}),
                    ("44 abc", new[] {("num", "44"), ("abc", "abc")}),
                    ("44 ab", new[] {("num", "44"), ("id", "ab")}),
                    ("44 abcd", new[] {("num", "44"), ("id", "abcd")}),
                    ("", new (string, string)[] { })
                }
            ),
            new TestContext(
                new Grammar(new[]
                {
                    "S -> A S | #",
                    "A -> a B",
                    "B -> b C b",
                    "C -> c C | #"
                }),
                new (string, string)[] { },
                new[]
                {
                    ("abccbabb",
                        new[]
                        {
                            ("a", "a"), ("b", "b"), ("c", "c"), ("c", "c"), ("b", "b"), ("a", "a"), ("b", "b"),
                            ("b", "b")
                        }),
                    ("abb", new[] {("a", "a"), ("b", "b"), ("b", "b")}),
                    ("abcb", new[] {("a", "a"), ("b", "b"), ("c", "c"), ("b", "b")}),
                    ("", new (string, string)[] { })
                }
            ),
            new TestContext(
                new Grammar(new[]
                {
                    "S -> A S | #",
                    "A -> if A | if A else A | ;",
                }),
                new (string, string)[] { },
                new[]
                {
                    ("if ;", new[] {("if", "if"), (";", ";")}),
                    ("if ; if ; else ;", new[] {("if", "if"), (";", ";"), ("if", "if"), (";", ";"), ("else", "else"), (";", ";")}),
                    ("if if ; else ;", new[] {("if", "if"), ("if", "if"), (";", ";"), ("else", "else"), (";", ";")})
                }
            ),
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
                    ("num", @"\b\d+\b"),
                    ("id", @"\b[a-z]+\b")
                },
                new (string, (string, string)[])[]
                {
                    ("4 #", null),
                    ("4 4 @foo", null),
                    ("4e", null),
                }
            )
        };

        private static TestContext[] _parseFailContexts =
        {
            new TestContext(
                new Grammar(new[]
                {
                    "S -> A B | #",
                    "A -> A num | num",
                    "B -> abc | id | #",
                }),
                new[]
                {
                    ("num", @"\b\d+\b"),
                    ("id", @"\b[a-z]+\b")
                },
                new[]
                {
                    ("4 foo 4", new[] {("num", "4"), ("foo", "foo"), ("num", "4")}),
                    ("foo", new[] {("id", "foo")}),
                    ("abc", new[] {("abc", "abc")}),
                    ("abc 4", new[] {("abc", "abc"), ("num", "4")}),
                }
            ),
            new TestContext(
                new Grammar(new[]
                {
                    "S -> A S | #",
                    "A -> a B",
                    "B -> b C b",
                    "C -> c C | #"
                }),
                new (string, string)[] { },
                new[]
                {
                    ("abccbab",
                        new[]
                        {
                            ("a", "a"), ("b", "b"), ("c", "c"), ("c", "c"), ("b", "b"), ("a", "a"), ("b", "b"),
                        }),
                    ("bb", new[] {("a", "a"), ("b", "b"), ("b", "b")}),
                    ("abbabbb", new[] {("a", "a"), ("b", "b"), ("b", "b"), ("a", "a"), ("b", "b"), ("b", "b"), ("b", "b")}),
                    ("abbab", new[] {("a", "a"), ("b", "b"), ("b", "b"), ("a", "a"), ("b", "b")}),
                    ("abbbb", new[] {("a", "a"), ("b", "b"), ("b", "b"), ("b", "b"), ("b", "b")})
                }
            ),
            new TestContext(
                new Grammar(new[]
                {
                    "S -> A S | #",
                    "A -> if A | if A else A | ;",
                }),
                new (string, string)[] { },
                new[]
                {
                    ("; if", new[] {(";", ";"), ("if", "if")}),
                    ("if else ;", new[] {("if", "if"),  ("else", "else"), (";", ";")})
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
        private static object[] _parseFailTestCases = ContextMap(_parseFailContexts);

        [TestCaseSource(nameof(_lexPassTestCases))]
        [Test, Category("Lexer")]
        public void LexPassTest(Grammar g, IEnumerable<(string, string)> patterns,
            string input, IEnumerable<(string Token, string Raw)> expectedTokens)
        {
            new Parser(g).Lex(input, patterns).Match(
                tokens => Assert.AreEqual(expectedTokens, tokens),
                ex => throw ex
            );
        }

        [TestCaseSource(nameof(_lexFailTestCases))]
        [Test, Category("Lexer")]
        public void LexFailTest(Grammar g, IEnumerable<(string, string)> patterns, string input,
            IEnumerable<(string Token, string Raw)> expectedTokens)
        {
            new Parser(g).Lex(input, patterns).Match(
                tokens => Assert.Fail("Expected failure but lexed successfully."),
                ex => Assert.Pass()
            );
        }

        private IEnumerable<(string Token, string Raw)> GetTokenSequence(Parser.TreeNode root)
        {
            if (root.Children.Count == 0)
            {
                if (root.Item.Current == "#")
                {
                    return new List<(string Token, string Raw)>();
                }
                else
                {
                    return new List<(string Token, string Raw)> {(Token: root.Item.Current, Raw: root.Token)};
                }
            }
            else
            {
                return root.Children.Aggregate<Parser.TreeNode, IEnumerable<(string Token, string Raw)>>(
                    new List<(string Token, string Raw)>(),
                    (a, c) => a.Concat(GetTokenSequence(c)));
            }
        }

        private bool VerifyChildrenCount(Parser.TreeNode root, Grammar gram)
        {
            if (root.Children.Count != root.Item.Rule.Count && root.Item.IsReduce())
            {
                return false;
            }

            foreach (var child in root.Children)
            {
                if (!VerifyChildrenCount(child, gram))
                {
                    return false;
                }
            }

            return true;
        }


        [TestCaseSource(nameof(_parsePassTestCases))]
        [Test, Category("Parser")]
        public void ParsePassTest(Grammar g, IEnumerable<(string, string)> patterns, string input,
            IEnumerable<(string Token, string Raw)> expectedTokens)
        {
            new Parser(g).Parse(input, patterns).Match(
                tree =>
                {
                    Assert.True(VerifyChildrenCount(tree, g));
                    Assert.AreEqual(expectedTokens, GetTokenSequence(tree));
                },
                ex => throw ex
            );
        }

        [TestCaseSource(nameof(_parseFailTestCases))]
        [Test, Category("Parser")]
        public void ParseFailTest(Grammar g, IEnumerable<(string, string)> patterns, string input,
            IEnumerable<(string Token, string Raw)> expectedTokens)
        {
            Assert.True(new Parser(g).Parse(input, patterns).IsFailure);
        }
    }
}