using Common.ParserFramework;
using Common.Tokens;

namespace Common.Tokenization.Rules
{
    class UrlCombinationRule : UrlReductionRule
    {
        private static readonly Predicate[] Pattern = new Predicate[] 
        {
            ClassType(typeof(UrlString)),
            ClassType(typeof(Token))
        };

        public override bool Apply(ReductionStack s, object la)
        {
            if(s.MatchRight(Pattern))
            {
                var url = s.Get<UrlString>(-2);
                var token = s.Get<Token>(-1);

                if (!IsTerminator(token))
                {
                    url.Extend(token);
                    s.Replace(2, url);
                    return true;
                }
            }

            return false;
        }
    }
}
