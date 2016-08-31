using Common.ParserFramework;
using Common.Tokens;

namespace Common.Tokenization.Rules
{
    class UrlTerminalRule : UrlReductionRule
    {
        public override bool Apply(ReductionStack s, object la)
        {
            if (IsTerminator(la))
            {
                var stack = (TokenizerReductionStack)s;
                stack.PopRules();
                return true;
            }

            return false;
        }
    }
}
