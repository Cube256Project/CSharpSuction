using Common.Tokens;
using System;
using System.Text.RegularExpressions;

namespace Common.Tokenization
{
    /// <summary>
    /// Constructs tokens.
    /// </summary>
    public static class TokenFactory
    {
        /// <summary>
        /// Constructs a token from a regular expression match.
        /// </summary>
        /// <param name="gname">The group name.</param>
        /// <param name="match">The match.</param>
        /// <returns>The resulting token.</returns>
        public static Token ConstructToken(string gname, Match match)
        {
            return CreateToken(gname)
                .AssignMatch(match);
        }

        public static Token CreateToken(string gname)
        {
            var ctype = typeof(Token);
            var type = ctype.Assembly.GetType(ctype.Namespace + "." + gname);
            if (null == type)
            {
                throw new Exception("token class [" + gname + "] was not found.");
            }

            return (Token)Activator.CreateInstance(type);
        }
    }
}
