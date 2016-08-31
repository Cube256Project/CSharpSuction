using Common.ParserFramework;
using Common.Tokenization.RuleSets;
using Common.Tokens;

namespace Common.Tokenization.Rules
{
    class EmitRule : ReductionRule
    {
        public override bool Apply(ReductionStack s, object la)
        {
            var stack = (TokenizerReductionStack)s;
            if (stack.Top.GetType() == typeof(DefaultRuleSet))
            {
                if (la is Whitespace || la is EndOfFile)
                {
                    stack.Emit();
                    return true;
                }
            }

            return false;
        }
    }
}
