using Common.ParserFramework;
using Common.Tokens;

namespace Common.Tokenization.Rules
{
    abstract class UrlReductionRule : ReductionRule
    {
        protected bool IsTerminator(object token)
        {
            return
                token is Semicolon
                || token is CommaSeparator
                || token is VerticalBar
                || token is Whitespace
                || token is EndOfFile;
        }
    }
}
