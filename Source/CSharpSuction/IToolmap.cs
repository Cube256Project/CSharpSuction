using CSharpSuction.Input;
using System.Collections.Generic;

namespace CSharpSuction
{
    public interface IToolmap
    {
        IEnumerable<AssemblyReferenceInfo> GetReferenceViaNamespace(string ns);
    }
}
