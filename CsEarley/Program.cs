using System;

namespace CsEarley
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var gram = new Grammar(new[]
            {
                "S -> A B C",
                "A -> a | #",
                "B -> A c | b",
                "C -> C e S | A | d"
            });
            Console.WriteLine(gram.ToString());
        }
    }
}