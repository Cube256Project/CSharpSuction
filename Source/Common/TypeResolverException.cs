using System;

namespace Common
{
    [Serializable]
    public class TypeResolverException : Exception
    {
        public TypeResolverException(string message)
            : base(message)
        { }
    }
}
