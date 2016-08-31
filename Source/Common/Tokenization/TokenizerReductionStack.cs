using Common.ParserFramework;
using Common.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Tokenization
{
    class TokenizerReductionStack : ReductionStack
    {
        public TokenReader Parent { get; private set; }

        public TokenizerReductionStack(TokenReader tokenizer)
        {
            Parent = tokenizer;
        }

        public override void PushRules(ReductionRuleCollection rules)
        {
            base.PushRules(rules);
            Parent.OnRulesChanged((TokenizerRuleCollection)rules);
        }

        public override void PopRules()
        {
            base.PopRules();

            if (null != Top)
            {
                Parent.OnRulesChanged((TokenizerRuleCollection)Top);
            }
        }

        public void ConvertRight<T>(int count)
        {
            var tokens = GetRange(Count - count, Count).OfType<Token>();
            var replace = Activator.CreateInstance(typeof(T), tokens);
            Replace(count, replace);
        }

        protected override bool ApplyRule(ReductionRule rule, object lookahead)
        {
            if (base.ApplyRule(rule, lookahead))
            {
                Parent.Trace("applied rule [{0,20}] to stack |{1}|", rule.Name, this.ToSeparatorList("|"));
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Emit()
        {
            Parent.Emit(this.OfType<Token>());
            Clear();
        }
    }
}
