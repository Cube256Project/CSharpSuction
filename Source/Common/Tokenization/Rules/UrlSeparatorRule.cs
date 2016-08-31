using Common.ParserFramework;
using Common.Tokens;
using System.Linq;

namespace Common.Tokenization.Rules
{
    class UrlSeparatorRule : ReductionRule
    {
        private static readonly Predicate[] Pattern = new Predicate[] 
        {
            ExactType(typeof(SimpleColonSeparator)),
            ExactType(typeof(ForwardSlash)),
            ExactType(typeof(ForwardSlash))
        };

        public override bool Apply(ReductionStack s, object la)
        {
            if (s.MatchRight(Pattern))
            {
                s.Replace(3, new UrlSeparator());
                return true;
            }

            return false;
        }
    }
}
