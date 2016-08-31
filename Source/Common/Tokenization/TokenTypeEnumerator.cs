using Common.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Tokenization
{
    public static class TokenTypeEnumerator
    {
        public static IEnumerable<Type> GetDefaultTokenTypes()
        {
            return typeof(Token).Assembly.GetTypes()
                .Where(t => typeof(Token).IsAssignableFrom(t))
                .Where(t => !typeof(NonDefault).IsAssignableFrom(t))
                .Where(t => !t.IsAbstract)
                ;
        }

        public static HashSet<Type> ExpandTokenType(Type type)
        {
            var types = TokenTypeEnumerator.GetDefaultTokenTypes().Where(t => type.IsAssignableFrom(t));
            return new HashSet<Type>(types);
        }

    }
}
