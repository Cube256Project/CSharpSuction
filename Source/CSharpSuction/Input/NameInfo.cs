using Microsoft.CodeAnalysis;

namespace CSharpSuction.Input
{
    class NameInfo : INameInfo
    {
        public NameRole Role { get; set; }

        public ISourceInfo Source { get; set; }

        public SyntaxNode Node { get; set; }

        public string QualifiedName { get { return Node.GetDeclaration(); } }

        public string Namespace { get { return Node.GetNamespace(); } }

        public bool IsExtensionMethod {  get { return Role == NameRole.ExtensionMethod; } }

        public bool IsTypeName { get { return Role == NameRole.Declaration; } }
    }
}
