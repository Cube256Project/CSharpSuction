using Common.ParserFramework;
using Common.Tokenization.Rules;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Common.Tokenization.RuleSets
{
    public class BasicRuleSet : TokenizerRuleCollection
    {
        private static Regex _basicexpression = 
            ExpressionFactory.ConstructExpression(TokenTypeEnumerator.GetDefaultTokenTypes());

        public override Regex Expression
        {
            get { return _basicexpression; }
        }

        public override IEnumerable<ReductionRule> Select(ReductionStack stack, object lookahead)
        {
            yield return new QuoteStartRule();
        }
    }
}
