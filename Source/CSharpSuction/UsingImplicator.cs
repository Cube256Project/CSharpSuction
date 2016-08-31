using CSharpSuction.Input;
using System.Collections.Generic;
using System.Web;

namespace CSharpSuction
{
    /// <summary>
    /// Resolves using references into assemblies, using the <see cref="IToolmap"/> .
    /// </summary>
    public class UsingImplicator
    {
        private IToolmap _toolmap;

        /// <summary>
        /// Creates a new using implicator based on a toolmap.
        /// </summary>
        /// <param name="toolmap"></param>
        public UsingImplicator(IToolmap toolmap)
        {
            _toolmap = toolmap;
        }

        /// <summary>
        /// Returns a set of assembly locations for a given namespace.
        /// </summary>
        /// <param name="ns"></param>
        /// <returns></returns>
        public IEnumerable<AssemblyReferenceInfo> GetAssemblyLocations(string ns)
        {
            if (ns == "System.Data")
            {

            }
            else if (ns.StartsWith("System.Web"))
            {
                var umm4 = new HtmlString("foo");
            }

            return _toolmap.GetReferenceViaNamespace(ns);
        }
    }
}
