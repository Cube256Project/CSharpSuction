using Common.ParserFramework;
using Common.Tokenization.Rules;
using System.Collections.Generic;

namespace Common.Tokenization.RuleSets
{
    public class DefaultRuleSet : BasicRuleSet
    {
        public override IEnumerable<ReductionRule> Select(ReductionStack stack, object lookahead)
        {
            foreach(var e in base.Select(stack, lookahead))
            {
                yield return e;
            }

            yield return new SeparatorCoercionRule();
            yield return new EmailAddressRule();
            yield return new UrlSeparatorRule();
            yield return new CommentStartRule();
            yield return new EmitRule();
        }
    }
}
