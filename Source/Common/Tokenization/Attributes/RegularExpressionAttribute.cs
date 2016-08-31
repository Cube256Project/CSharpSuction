using System;
using System.Text.RegularExpressions;

namespace Common.Tokenization.Attributes
{
    /// <summary>
    /// Attributes a regular expression to an atomic token.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RegularExpressionAttribute : Attribute
    {
        public string Expression { get; private set; }

        /// <summary>
        /// Priority of the rule; lower values have a higher priority.
        /// </summary>
        /// <remarks>
        /// <para>The default value is 10.</para></remarks>
        public int Priority { get; private set; }

        public RegularExpressionAttribute(string e, int priority = 10)
        {
            Expression = e;
            Priority = priority;
        }
    }
}
