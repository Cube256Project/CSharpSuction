using System.Collections.Generic;

namespace Common.ParserFramework
{
    /// <summary>
    /// A collection of reduction rules representing a syntax.
    /// </summary>
    public abstract class ReductionRuleCollection
    {
        /// <summary>
        /// Selects a set of rules based on the current state stack and the lookahead symbol.
        /// </summary>
        /// <param name="stack">The reduction stack holding the state.</param>
        /// <param name="lookahead">The lookahead symbol.</param>
        /// <returns>A set of reduction rules that may be applied to the stack.</returns>
        /// <remarks>
        /// <para>The implementation is free to filter the returned rules or not, depending
        /// on the needs an size of the syntax. For simple syntaxes it may be sufficient to 
        /// just return all the rules.</para>
        /// </remarks>
        public abstract IEnumerable<ReductionRule> Select(ReductionStack stack, object lookahead);
    }
}
