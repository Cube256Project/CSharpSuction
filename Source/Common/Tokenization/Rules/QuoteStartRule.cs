using Common.ParserFramework;
using Common.Tokenization.RuleSets;
using Common.Tokens;

namespace Common.Tokenization.Rules
{
    class QuoteStartRule : ReductionRule
    {
        public override bool Apply(ReductionStack s, object la)
        {
            var stack = (TokenizerReductionStack)s;

            Quote quote;
            if (s.TryGet<Quote>(-1, out quote))
            {
                var xml = stack.Top is XmlRuleSet;

                // WEKKX4UYXK: switch into quoted content rules
                stack.PushRules(new QuotedContentRuleSet(quote, xml));
                stack.EndReduce();
                return true;
            }

            return false;
        }
    }
}
