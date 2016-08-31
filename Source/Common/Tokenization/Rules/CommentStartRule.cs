using Common.ParserFramework;
using Common.Tokenization.RuleSets;
using Common.Tokens;

namespace Common.Tokenization.Rules
{
    class CommentStartRule : ReductionRule
    {
        #region Private

        private static readonly Predicate[] Pattern1 = new Predicate[]
        {
            ClassType(typeof(ForwardSlash)),
            ClassType(typeof(ForwardSlash))
        };

        private static readonly Predicate[] Pattern2 = new Predicate[]
        {
            ClassType(typeof(ForwardSlash)),
            ClassType(typeof(Asterisk))
        };

        #endregion

        public override bool Apply(ReductionStack s, object la)
        {
            if (s.MatchRight(Pattern1))
            {
                s.Replace(2, new SingleLineCCommentInitiator());
                s.PushRules(new SingleLineCommentRuleSet());
                s.EndReduce();
                return true;
            }
            else if(s.MatchRight(Pattern2))
            {
                s.Replace(2, new MultiLineCCommentInitiator());
                s.PushRules(new MultiLineCommentRuleSet());
            }

            return false;
        }
    }
}
