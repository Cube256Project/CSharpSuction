using Common.ParserFramework;
using Common.Tokens;

namespace Common.Tokenization.Rules
{
    class SingleLineCommentSuffixRule : ReductionRule
    {
        public override bool Apply(ReductionStack s, object la)
        {
            var stack = (TokenizerReductionStack)s;

            if (la is LineBreak)
            {
                var n = s.Count;
                var j = n;
                while (j > 0)
                {
                    --j;
                    if (s.Match<SingleLineCCommentInitiator>(j))
                    {
                        stack.ConvertRight<SingleLineCComment>(n - j);
                        stack.PopRules();
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
