using System.Collections.Generic;

namespace CSharpSuction
{
    public interface ITypeInfo
    {
        string QualifiedName { get; }

        IEnumerable<ISourceInfo> Sources { get; }
    }
}
