using System;

namespace CsEarley
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var gram = new Grammar(new[]
            {
                "S -> A S | #",
                "A -> if A | if A else A | ;",
            });

            var parser = new Parser(gram);
            var tokens = parser.Lex("if if ; else ;", new[] {("num", "[0-9]+")}).Match(
                list => list,
                ex => throw ex
            );
            var tree = parser.Parse(tokens);
            Console.WriteLine("foo");
        }
    }
}