using Common.Tokenization.Attributes;

namespace Common.Tokens
{
    [RegularExpression(@"\=")]
    public sealed class EqualSign : Atomic { }

    [RegularExpression(@"\;")]
    public sealed class Semicolon : Atomic { }

    [RegularExpression(@"\*")]
    public sealed class Asterisk : Atomic { }

    [RegularExpression(@"\?")]
    public sealed class QuestionMark : Atomic { }

    [RegularExpression(@"\&")]
    public sealed class Ampersand : Atomic { }

    [RegularExpression(@"\@")]
    public sealed class AtSign : Atomic { }

    [RegularExpression(@"\$")]
    public sealed class Dollar : Atomic { }

    [RegularExpression(@"\~")]
    public sealed class Tilde : Atomic { }

    [RegularExpression(@"\|")]
    public sealed class VerticalBar : Atomic { }

    [RegularExpression(@"\!")]
    public sealed class ExclamationMark : Atomic { }

    [RegularExpression(@"\#")]
    public sealed class Sharp : Atomic { }

    [RegularExpression(@"\+")]
    public sealed class PlusSign : Atomic { }

    [RegularExpression(@"\·")]
    public sealed class MiddleDotSeparator : Atomic { }

}
