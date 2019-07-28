using System;
using System.Text.RegularExpressions;

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
            var tree = parser.Parse(Regex.Split("number + number * number", "\\s+"));
            Console.WriteLine("foo");
        }
    }
}