using Common.Tokenization;
using Common.Tokenization.Locations;
using System.Text.RegularExpressions;

namespace Common.Tokens
{
    /// <summary>
    /// The root of the token class hierarchy.
    /// </summary>
    public abstract class Token
    {
        #region Protected

        protected string _text;

        #endregion

        #region Properties

        /// <summary>
        /// Returns the textual value of the token.
        /// </summary>
        public virtual string Value
        {
            get { return GetText(); }
        }

        public string Kind { get { return GetType().Name; } }

        public ITokenLocation Location { get; private set; }

        #endregion

        #region Diagnostics

        public override string ToString()
        {
            var value = Value;
            if (value.Length > 0) return Value;
            else return "<" + GetType().Name + ">";
        }

        #endregion

        #region Public Methods

        public bool Match(string text)
        {
            return 0 == string.Compare(Value, text, true);
        }

        #endregion

        #region Overrideables

        protected virtual string GetText()
        {
            if (null == _text) SetDefaultText();
            return _text;
        }

        protected virtual void SetDefaultText()
        {
            _text = string.Empty;
        }

        internal virtual Token AssignMatch(Match match)
        {
            _text = match.Value;
            return this;
        }

        protected virtual void SetLocation(Token start, Token end)
        {
            Location = new ImmutableTokenLocation(start, end);
        }

        public virtual Token WithLocation(ITokenLocation location)
        {
            Location = new ImmutableTokenLocation(location);
            return this;
        }

        internal Token WithLocation(Token token)
        {
            return null == token || null == token.Location ? this : WithLocation(token.Location);
        }

        #endregion
    }
}
