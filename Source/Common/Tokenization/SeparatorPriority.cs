
namespace Common.Tokenization
{
    /// <summary>
    /// Describes the separator priority of a token.
    /// </summary>
    public enum SeparatorPriority
    {
        lowest,
        endofline,
        whitespace,
        url,
        path,
        asset,
        dot,
        dash,
        higest,
    }
}
