using System;

namespace CsEarley
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var gram = new Grammar(new[]
            {
                "S -> S + M | M",
                "M -> M * T | T",
                "T -> number"
            });
            var parser = new Parser(gram);
            Console.WriteLine(parser.Recognize("number + number * number"));
            Console.WriteLine("foo");
        }
    }
}