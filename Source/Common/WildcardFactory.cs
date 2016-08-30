using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Common
{
    /// <summary>
    /// Converts file name masks into regular expressions.
    /// </summary>
    public static class WildcardFactory
    {
        /// <summary>
        /// Builds a regular expression from a list of wildcards.
        /// </summary>
        /// <param name="wildcards">List of wildcard strings.</param>
        /// <returns>A regular expression matching the specified wildcards.</returns>
        public static Regex BuildWildcards(IEnumerable<string> wildcards)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("^(");
            bool first = true;
            foreach (string w in wildcards)
            {
                if (!first) sb.Append('|'); else first = false;
                foreach (char c in w)
                {
                    if (c == '*') { sb.Append(".*"); }
                    else if (c == '?') { sb.Append("."); }
                    else
                    {
                        sb.Append(Regex.Escape(c.ToString()));
                    }
                }
            }

            sb.Append(")$");
            return new Regex(sb.ToString(), RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Builds a regular expression from a separated list of wildcards.
        /// </summary>
        /// <param name="list">String separated by ';' or ',' or space.</param>
        /// <returns>A regular expression matching the specified wildcards.</returns>
        public static Regex BuildWildcardsFromList(string list)
        {
            return BuildWildcards(list.Split(';', ',', ' ', '|')
                .Select(u => u.Trim())
                .Where(u => u.Length > 0));
        }

        public static Regex BuildWildcards(string wildcard)
        {
            return BuildWildcards(new string[] { wildcard });
        }
    }
}
