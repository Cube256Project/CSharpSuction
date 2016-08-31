using Common.Tokenization.Attributes;

namespace Common.Tokens
{
    public abstract class Whitespace : Atomic { }

    [RegularExpression(@"[ \t]+")]
    public sealed class InlineWhitespace : Whitespace { }

    [RegularExpression(@"[\r\n]+")]
    public sealed class LineBreak : Whitespace { }
}
