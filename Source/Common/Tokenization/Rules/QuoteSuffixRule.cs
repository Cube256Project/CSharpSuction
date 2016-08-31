using Common.ParserFramework;
using Common.Tokens;
using System;
using System.Linq;

namespace Common.Tokenization.Rules
{
    class QuoteSuffixRule : ReductionRule
    {
        private Quote _leftquote;

        public QuoteSuffixRule(Quote quote)
        {
            _leftquote = quote;
        }

        public override bool Apply(ReductionStack s, object la)
        {
            var stack = (TokenizerReductionStack)s;

            Quote quote;
            if(s.TryGet(-1, out quote))
            {
                // WEKKX4UYXK: complete quotes
                if (_leftquote.IsClosingQuote(quote))
                {
                    var n = s.Count;
                    var j = n - 1;
                    while (j > 0)
                    {
                        --j;
                        if(stack[j] == _leftquote)
                        {
                            break;
                        }
                    }

                    var content = stack.GetRange(j, s.Count).OfType<Token>();
                    var replace = QuotedString.Create(content).WithLocation(_leftquote.Location);

                    stack.Replace(n - j, replace);
                    stack.PopRules();
                    return true;
                }
            }

            return false;
        }
    }
}
