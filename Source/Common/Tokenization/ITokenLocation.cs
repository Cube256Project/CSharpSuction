
namespace Common.Tokenization
{
    /// <summary>
    /// Desribes the location of a token within the parsing context.
    /// </summary>
    public interface ITokenLocation
    {
        /// <summary>
        /// The base uri of the document being parsed.
        /// </summary>
        string BaseUri { get; }

        /// <summary>
        /// Zero based character offset.
        /// </summary>
        int StartPosition { get; }

        int EndPosition { get; }

        /// <summary>
        /// Zero-based line index.
        /// </summary>
        int Line { get; }

        /// <summary>
        /// Zero-based column index.
        /// </summary>
        int Column { get; }
    }

}
