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
                "E -> E + T | T",
                "T -> T * F | F",
                "F -> ( E ) | num"
            });
            var parser = new Parser(gram);
            var tree = parser.Parse(Regex.Split("( num + num ) * num", "\\s+"));
            Console.WriteLine("foo");
        }
    }
}