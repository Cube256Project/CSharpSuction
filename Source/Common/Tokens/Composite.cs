using System.Collections.Generic;
using System.Linq;
using Common.Tokenization;

namespace Common.Tokens
{
    public abstract class Composite : Token
    {
        #region Private

        protected Token[] _elements;

        #endregion

        /// <summary>
        /// Returns all elements of this composite token.
        /// </summary>
        public virtual IEnumerable<Token> Elements { get { return _elements; } }

        public Composite()
        {
        }

        protected Composite(IEnumerable<Token> tokens)
        {
            SetElements(tokens);
            SetLocation(tokens.First(), tokens.Last());
        }

        internal void SetElements(IEnumerable<Token> tokens, TypeSelector expand = null)
        {
            expand = expand ?? new TypeSelector(t => false);
            _elements = tokens.ToArray();
            _text = tokens.Expand(expand).ToSeparatorList("");

            SetLocation(_elements.First(), _elements.Last());
        }
    }
}
