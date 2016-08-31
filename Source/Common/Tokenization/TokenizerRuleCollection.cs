using Common.ParserFramework;
using System.Text.RegularExpressions;

namespace Common.Tokenization
{
    public abstract class TokenizerRuleCollection : ReductionRuleCollection
    {
        /// <summary>
        /// Optional regular expression associated with this rule-set.
        /// </summary>
        /// <remarks>
        /// If unspecified, the parent expression shall be used.</remarks>
        public abstract Regex Expression { get; }
    }
}
