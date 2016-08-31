using System.Collections.Generic;
using System.Linq;

namespace Common.Tokens
{
    public abstract class SeparatedString : Composite, GeneralString
    {
        /// <summary>
        /// Returns the value tokens, without the separators.
        /// </summary>
        public IEnumerable<Token> Values { get { return _elements.Where(t => !(t is Separator)); } }

        /// <summary>
        /// Combines a string-separator-string sequence into a separated string token.
        /// </summary>
        /// <param name="tokens">A collection of three tokens.</param>
        protected SeparatedString(IEnumerable<Token> tokens)
        {
            var args = tokens.ToArray();
            var left = args[0];
            var separator = args[1];
            var right = args[2];

            if (GetType().IsAssignableFrom(left.GetType()))
            {
                _elements = ((SeparatedString)left)._elements.Concat(new[] { separator, right }).ToArray();
            }
            else
            {
                _elements = new Token[] { left, separator, right };
            }

            WithLocation(left);

            _text = _elements.Select(e => e.Value).ToSeparatorList("");
        }

    }

    /// <summary>
    /// String with elements separated by dots.
    /// </summary>
    public sealed class DottedString : SeparatedString, InternetString, XmlSimpleName
    {
        public DottedString(IEnumerable<Token> tokens)
            : base(tokens)
        { }
    }

    /// <summary>
    /// String with elements separated by dashes.
    /// </summary>
    public sealed class DashedString : SeparatedString, XmlSimpleName
    {
        public DashedString(IEnumerable<Token> tokens)
            : base(tokens)
        { }
    }

    /// <summary>
    /// String with elements separated by path separators.
    /// </summary>
    public sealed class PathString : SeparatedString
    {
        public PathString(IEnumerable<Token> tokens)
            : base(tokens)
        { }
    }

    public sealed class UrlString : Composite, InternetString
    {
        public UrlString(IEnumerable<Token> tokens) : base(tokens) { }

        public void Extend(params Token[] tokens)
        {
            SetElements(Elements.Concat(tokens));
        }
    }

    public sealed class EmailAddress : Composite, InternetString
    {
        public EmailAddress(IEnumerable<Token> tokens) : base(tokens) { }
    }
}
