
using Common.Tokenization.Attributes;
namespace Common.Tokens
{
    public abstract class Bracket : Atomic { }

    public abstract class BracketOpen : Bracket { }

    public abstract class BracketClose : Bracket { }

    [RegularExpression(@"\(")]
    public sealed class ParenthesisLeft : BracketOpen { }

    [RegularExpression(@"\)")]
    public sealed class ParenthesisRight : BracketClose { }

    [RegularExpression(@"\[")]
    public sealed class SquareBracketLeft : BracketOpen { }

    [RegularExpression(@"\]")]
    public sealed class SquareBracketRight : BracketClose { }

    [RegularExpression(@"\{")]
    public sealed class CurlyBracketLeft : BracketOpen { }

    [RegularExpression(@"\}")]
    public sealed class CurlyBracketRight : BracketClose { }

    [RegularExpression(@"\<")]
    public sealed class AngleBracketLeft : BracketOpen
    {
        protected override void SetDefaultText()
        {
            _text = "<";
        }
    }

    [RegularExpression(@"\>")]
    public sealed class AngleBracketRight : BracketClose { }
}
