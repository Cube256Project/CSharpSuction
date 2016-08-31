using Common.ParserFramework;
using Common.Tokenization.RuleSets;
using Common.Tokens;
using System.Linq;

namespace Common.Tokenization.Rules
{
    class SeparatorCoercionRule : ReductionRule
    {
        #region Private

        private static readonly Predicate[] Pattern = new Predicate[]
        {
            ClassType(typeof(GeneralString)),
            ClassType(typeof(Separator)), 
            ClassType(typeof(GeneralString))
        };

        #endregion

        public override bool Apply(ReductionStack s, object la)
        {
            if(s.MatchRight(Pattern))
            {
                // separator priority must be greater equal reduction.
                var separatortoken=s.Get<Separator>(-2);
                var separator = separatortoken.GetSeparatorPriority();
                var lookahead = ((Token)la).GetSeparatorPriority();

                var left = s.Get<Token>(-3);

                // combine if previous separator priority is higher than the lookahead
                var combine = separator >= lookahead;

                if (combine)
                {
                    var replace = separatortoken.CombineTokens(s.Skip(s.Count - 3).OfType<Token>());
                    s.Replace(3, replace);

                    if(replace is UrlString)
                    {
                        // push rules
                        s.PushRules(new UrlRuleSet());
                        s.EndReduce();
                    }

                    return true;
                }
            }

            return false;
        }
    }
}
