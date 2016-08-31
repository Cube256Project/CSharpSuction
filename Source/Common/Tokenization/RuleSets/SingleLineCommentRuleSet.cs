using Common.ParserFramework;
using Common.Tokenization.Rules;
using System.Collections.Generic;
using System.Linq;

namespace Common.Tokenization.RuleSets
{
    class SingleLineCommentRuleSet : DefaultRuleSet
    {
        public override IEnumerable<ReductionRule> Select(ReductionStack stack, object lookahead)
        {
            return base.Select(stack, lookahead)
                .Concat(new[] { new SingleLineCommentSuffixRule() });
        }
    }
}
