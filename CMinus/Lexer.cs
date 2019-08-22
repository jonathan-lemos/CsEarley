using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using CsEarley;
using CsEarley.Functional;

namespace CMinus
{
    public class Lexer
    {
        public static Try<string, ValuedException<string>> StripComments(string input)
        {
            var matcher = new Regex(@"/\*|\*/|//");
            var commentCtr = 0;
            var res = string.Join('\n', input.Split("\n").Select(line =>
            {
                var pos = 0;
                var ret = "";
                Match match;

                while ((match = matcher.Match(line, pos)).Success)
                {
                    if (commentCtr == 0)
                    {
                        ret += line.Substring(pos, match.Index - pos);
                    }

                    pos = match.Index + match.Length;

                    if (match.Value == "/*")
                    {
                        commentCtr++;
                    }
                    else if (match.Value == "*/")
                    {
                        if (commentCtr > 0)
                        {
                            commentCtr--;
                        }
                        else
                        {
                            ret += "*/";
                        }
                    }
                    else if (match.Value == "//")
                    {
                        if (commentCtr == 0)
                        {
                            return ret;
                        }
                    }
                    else
                    {
                        Debug.Fail($"Invalid match.Value '{match.Value}'");
                    }
                }

                if (commentCtr == 0)
                {
                    ret += line.Substring(pos);
                }

                return ret;
            }));

            if (commentCtr > 0)
            {
                return new ValuedException<string>("Expected '*/' but reached EOF", res);
            }

            return res;
        }

        public static Either<Try<IList<(string Token, string Raw)>, ValuedException<IList<(string Token, string Raw)>>>,
            ValuedException<string>> Lex(
            string input) =>
            StripComments(input)
                .Match<Either<Try<IList<(string Token, string Raw)>, ValuedException<IList<(string Token, string Raw)>>>
                    ,
                    ValuedException<string>>>(
                    success => new Parser(new Grammar(CmGrammar.Grammar)).Lex(success, CmGrammar.Patterns),
                    ex => ex
                );
    }
}