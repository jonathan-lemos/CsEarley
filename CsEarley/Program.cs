using System;

namespace CsEarley
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var gram = new Grammar(new[]
            {
                "E -> E + T | T",
                "T -> T * F | F",
                "F -> ( E ) | num"
            });

            var parser = new Parser(gram);
            var tokens = parser.Lex("  (2+ 3 ) * 4  ", new[] {("num", "[0-9]+")}).Match(
                list => list,
                ex => throw ex
            );
            var tree = parser.Parse(tokens);
            Console.WriteLine("foo");
        }
    }
}