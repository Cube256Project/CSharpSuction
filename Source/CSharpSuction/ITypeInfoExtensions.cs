using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace CSharpSuction
{
    public static class ITypeInfoExtensions
    {
        public static IEnumerable<SyntaxNode> Nodes(this ITypeInfo typeinfo)
        {
            return ((TypeInfo)typeinfo).Nodes;
        }
    }
}
