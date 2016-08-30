using CSharpSuction.Input;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace CSharpSuction
{
    /// <summary>
    /// Provides information about a data-type found by the suction.
    /// </summary>
    class TypeInfo : ITypeInfo
    {
        #region Private

        private INamedTypeSymbol _symbol;
        private string _qname;
        private List<SourceInfo> _sources = new List<SourceInfo>();
        private HashSet<SyntaxNode> _nodes = new HashSet<SyntaxNode>();

        #endregion

        #region Properties

        public string QualifiedName { get { return _qname; } }

        public IEnumerable<ISourceInfo> Sources { get { return _sources; } }

        public INamedTypeSymbol Symbol {  get { return _symbol; } }

        /// <summary>
        /// The syntax node(s) declaring the type; multiple in case of partial classes.
        /// </summary>
        public IEnumerable<SyntaxNode> Nodes { get { return _nodes; } }

        #endregion

        public TypeInfo(INamedTypeSymbol symbol)
        {
            _symbol = symbol;
            _qname = symbol.ContainingNamespace + "." + symbol.Name;
        }

        public void Add(SourceInfo source)
        {
            if (!_sources.Contains(source))
            {
                _sources.Add(source);
            }
        }

        public void Add(SyntaxNode declaration)
        {
            _nodes.Add(declaration);
        }
    }
}
