namespace Common.ParserFramework
{
    /// <summary>
    /// A reduction rule operating on a <see cref="ReductionStack"/>.
    /// </summary>
    public abstract class ReductionRule : PredicateFactory
    {
        #region Diagnostics

        public virtual string Name { get { return GetType().Name; } }

        #endregion

        /// <summary>
        /// Try to apply this action to the current state stack.
        /// </summary>
        /// <param name="s">The stack to apply the rule to.</param>
        /// <param name="la">The current look-ahead symbol.</param>
        /// <returns>True if the rule was applied, false otherwise.</returns>
        public abstract bool Apply(ReductionStack s, object la);
    }
}
