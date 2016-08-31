using Common.ParserFramework;
using Common.Tokenization.Rules;
using Common.Tokens;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Common.Tokenization.RuleSets
{
    class QuotedContentRuleSet : DefaultRuleSet
    {
        private Quote _quote;
        private bool _forxml;

        public override Regex Expression
        {
            get
            {
                // prepend escape expression
                return ExpressionFactory.CombineExpression(
                    new TokenTypeInfo(_quote.GetEscapeQuote().GetType()), 
                    base.Expression);
            }
        }

        public QuotedContentRuleSet(Quote quote, bool forxml)
        {
            _quote = quote;
            _forxml = forxml;
        }

        public override IEnumerable<ReductionRule> Select(ReductionStack stack, object lookahead)
        {
            yield return new QuoteSuffixRule(_quote);

            if(_forxml)
            {
                yield return new EntityReferenceRule();
            }

            /*foreach(var e in base.Select(stack, lookahead))
            {
                if(e is QuoteStartRule)
                if (!(e is QuoteStartRule))
                {
                    yield return e;
                }
            }*/ 
        }
    }
}
