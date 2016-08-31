using Common.ParserFramework;
using Common.Tokenization.Rules;
using System.Collections.Generic;

namespace Common.Tokenization.RuleSets
{
    class UrlRuleSet : BasicRuleSet
    {
        public override IEnumerable<ReductionRule> Select(ReductionStack stack, object lookahead)
        {
            yield return new SeparatorCoercionRule();
            yield return new UrlCombinationRule();
            yield return new UrlTerminalRule();
        }
    }
}
