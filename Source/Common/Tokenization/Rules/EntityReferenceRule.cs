using Common.ParserFramework;
using Common.Tokens;

namespace Common.Tokenization.Rules
{
    class EntityReferenceRule : ReductionRule
    {
        private static readonly Predicate[] DecimalFormat = new Predicate[] 
        {
            ClassType(typeof(Ampersand)),
            ClassType(typeof(Sharp)),
            ClassType(typeof(DecimalInteger)),
            ClassType(typeof(Semicolon))
        };

        private static readonly Predicate[] IdentifierFormat = new Predicate[] 
        {
            ClassType(typeof(Ampersand)),
            ClassType(typeof(Identifier)),
            ClassType(typeof(Semicolon))
        };

        public override bool Apply(ReductionStack s, object la)
        {
            var stack = (TokenizerReductionStack)s;

            if (s.MatchRight(DecimalFormat))
            {
                stack.ConvertRight<EntityReference>(4);
                return true;
            }
            else if (s.MatchRight(IdentifierFormat))
            {
                stack.ConvertRight<EntityReference>(3);
                return true;
            }

            return false;
        }
    }
}
