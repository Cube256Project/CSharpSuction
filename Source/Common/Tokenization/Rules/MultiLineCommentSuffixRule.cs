using Common.ParserFramework;
using Common.Tokens;

namespace Common.Tokenization.Rules
{
    class MultiLineCommentSuffixRule : ReductionRule
    {
        private static readonly Predicate[] Pattern3 = new Predicate[]
        {
            ClassType(typeof(Asterisk)),
            ClassType(typeof(ForwardSlash))
        };


        public override bool Apply(ReductionStack s, object la)
        {
            var stack = (TokenizerReductionStack)s;

            if (s.MatchRight(Pattern3))
            {
                s.Replace(2, new MultiLineCCommentTerminator());

                var n = s.Count;
                var j = n - 1;
                while(j > 0)
                {
                    --j;
                    if (s.Match<MultiLineCCommentInitiator>(j))
                    {
                        stack.ConvertRight<MultiLineCComment>(n - j);
                        stack.PopRules();
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
