using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Common.Tokenization
{
    /// <summary>
    /// Constructs a regular expression based on a set of token types.
    /// </summary>
    public static class ExpressionFactory
    {
        public static Regex ConstructExpression(IEnumerable<Type> tokentypes)
        {
            // TODO: optimize; cache regex per typeset (ordered?)
            var infos = tokentypes
                .Select(t => new TokenTypeInfo(t))
                .Where(e => e.Attribute != null)
                .OrderBy(e => e.Attribute.Priority);

            var r = new StringBuilder();

            WriteExpressions(r, infos);

            // Log.Trace("constructed expression: {0}", r);

            return new Regex(r.ToString(), RegexOptions.IgnoreCase);
        }

        private static void WriteExpressions(StringBuilder r, IEnumerable<TokenTypeInfo> infos)
        {
            foreach (var e in infos)
            {
                if (r.Length > 0) r.Append("|");
                r.Append("(?<" + e.TokenType.Name + ">");
                r.Append(e.Attribute.Expression);
                r.Append(")");
            }
        }

        public static Regex CombineExpression(TokenTypeInfo info, Regex existing)
        {
            var r = new StringBuilder();

            WriteExpressions(r, new[] { info });

            r.Append("|");
            r.Append(existing.ToString());

            return new Regex(r.ToString(), RegexOptions.IgnoreCase);
        }
    }
}
