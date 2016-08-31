using Common.ParserFramework;
using Common.Tokens;
using System.Linq;

namespace Common.Tokenization.Rules
{
    class EmailAddressRule : ReductionRule
    {
        #region Private

        private static readonly Predicate[] Pattern = new Predicate[]
        {
            ClassType(typeof(GeneralString)),
            ClassType(typeof(AtSign)), 
            ClassType(typeof(DottedString))
        };

        #endregion

        public override bool Apply(ReductionStack s, object la)
        {
            if(la is DotSeparator)
            {
                return false;
            }

            if (s.MatchRight(Pattern))
            {
                var replace = new EmailAddress(s.Skip(s.Count - 3).OfType<Token>());
                s.Replace(3, replace);
                return true;
            }

            return false;
        }
    }
}
