namespace CSharpSuction.Configuration
{
    /// <summary>
    /// Describes a source of a suction project.
    /// </summary>
    public class SourceInclude
    {
        /// <summary>
        /// The absolute path of the directory or file.
        /// </summary>
        public string AbsolutePath;

        /// <summary>
        /// Filter string according to <see cref="WildcardFactory"/>.
        /// </summary>
        public string Filter;
    }
}
