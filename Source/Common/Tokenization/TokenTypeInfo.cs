using Common.Tokenization.Attributes;
using System;
using System.Reflection;

namespace Common.Tokenization
{
    public class TokenTypeInfo
    {
        public Type TokenType { get; private set; }

        public RegularExpressionAttribute Attribute { get; private set; }

        public TokenTypeInfo(Type type)
        {
            TokenType = type;
            Attribute = type.GetCustomAttribute<RegularExpressionAttribute>();
        }
    }
}
