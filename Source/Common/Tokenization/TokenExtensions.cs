using Common.Tokens;
using System.Collections.Generic;
using System.Linq;

namespace Common.Tokenization
{
    public static class TokenExtensions
    {
        /// <summary>
        /// Expands a token sequence by decomposing selected tokens.
        /// </summary>
        /// <param name="tokens">The token sequence.</param>
        /// <param name="selector">Selects the tokens to decompose.</param>
        /// <param name="maxdepth">Maximum nesting level.</param>
        /// <returns>The decomposed token sequence.</returns>
        public static IEnumerable<Token> Expand(this IEnumerable<Token> tokens, TypeSelector selector = null, int maxdepth = int.MaxValue)
        {
            foreach (var t in tokens)
            {
                var activeselector = selector ?? new TypeSelector(a => true);
                if (t is Composite && activeselector(t.GetType()) && maxdepth > 0)
                {
                    var composite = (Composite)t;
                    foreach (var e in composite.Elements.Expand(selector, maxdepth - 1))
                    {
                        yield return e;
                    }
                }
                else
                {
                    yield return t;
                }
            }
        }

        public static IEnumerable<Token> ToElements(this Token token, TypeSelector selector = null)
        {
            return new[] { token }.Expand(selector);
        }

        public static IEnumerable<Token> Content(this IEnumerable<Token> tokens)
        {
            return tokens.Where(t => IsContent(t));
        }

        public static string ToText(this IEnumerable<Token> tokens)
        {
            return tokens.Where(t => !(t is FileDelimiter)).Select(t => t.Value).ToSeparatorList(" ");
        }

        private static bool IsContent(this Token t)
        {
            if (t is Whitespace)
            {
                return false;
            }
            else if (t is FileDelimiter)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static IEnumerable<Token> WithDelimiters(this IEnumerable<Token> tokens)
        {
            var sawbof = false;
            var saweof = false;
            var it = tokens.GetEnumerator();
            Token t = null;

            while (it.MoveNext())
            {
                t = it.Current;

                if (t is StartOfFile)
                {
                    sawbof = true;
                    yield return t;
                    continue;
                }

                if (!sawbof)
                {
                    sawbof = true;
                    yield return new StartOfFile().WithLocation(t);
                }

                yield return t;

                if (t is EndOfFile)
                {
                    saweof = true;
                }
            }

            if (!sawbof)
            {
                yield return new StartOfFile();
            }

            if (!saweof)
            {
                yield return new EndOfFile().WithLocation(t);
            }
        }

        public static SeparatorPriority GetSeparatorPriority(this Token token)
        {
            if (token is Identifier)
            {
                return SeparatorPriority.higest;
            }
            else if(token is Separator)
            {
                if (token is DashSeparator)
                {
                    return SeparatorPriority.dash;
                }
                else if (token is DotSeparator)
                {
                    return SeparatorPriority.dot;
                }
                else if (token is PathSeparator)
                {
                    return SeparatorPriority.path;
                }
                else if (token is ColonSeparator)
                {
                    return SeparatorPriority.url;
                }
                else
                {
                    return SeparatorPriority.whitespace;
                }
            }
            else if (token is LineBreak)
            {
                return SeparatorPriority.endofline;
            }
            else if (token is Whitespace)
            {
                return SeparatorPriority.whitespace;
            }
            else
            {
                return SeparatorPriority.lowest;
            }
        }
    }
}
