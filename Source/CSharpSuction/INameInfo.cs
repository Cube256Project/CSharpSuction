namespace CSharpSuction
{
    /// <summary>
    /// Contains information about an identifier name.
    /// </summary>
    public interface INameInfo
    {
        /// <summary>
        /// The source object the name is related to.
        /// </summary>
        ISourceInfo Source { get; }

        /// <summary>
        /// The namespace.
        /// </summary>
        string Namespace { get; }

        /// <summary>
        /// The fully qualified name.
        /// </summary>
        string QualifiedName { get; }

        bool IsExtensionMethod { get; }

        /// <summary>
        /// True if the name refers to a type.
        /// </summary>
        bool IsTypeName { get; }
    }
}
