using Common.Tokenization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Tokens
{
    public abstract class Quote : Atomic
    {
        public abstract EscapeSequence GetEscapeQuote();

        public virtual bool IsClosingQuote(Quote quote)
        {
            return quote.GetType() == GetType();
        }
    }

    public abstract class SingleQuote : Quote
    {
        public override EscapeSequence GetEscapeQuote()
        {
            return new SingleQuoteEscape();
        }
    }

    [RegularExpression(@"'")]
    public sealed class SimpleSingleQuote : SingleQuote
    {
        protected override void SetDefaultText()
        {
            _text = "'";
        }
    }

    [RegularExpression(@"b'", 9)]
    public sealed class Base64Quote : SingleQuote
    {
        protected override void SetDefaultText()
        {
            _text = "b'";
        }

        public override bool IsClosingQuote(Quote quote)
        {
            return quote.GetType() == typeof(SimpleSingleQuote);
        }
    }

    [RegularExpression(@"""")]
    public sealed class DoubleQuote : Quote
    {
        protected override void SetDefaultText()
        {
            _text = "\"";
        }

        public override EscapeSequence GetEscapeQuote()
        {
            return new DoubleQuoteEscape();
        }

    }

    public abstract class EscapeSequence : Atomic
    {
        public abstract Token Unescape();
    }

    [RegularExpression(@"''")]
    public sealed class SingleQuoteEscape : EscapeSequence, NonDefault
    {
        public override Token Unescape()
        {
            return new SimpleSingleQuote();
        }
    }

    [RegularExpression(@"""""")]
    public sealed class DoubleQuoteEscape : EscapeSequence, NonDefault
    {
        public override Token Unescape()
        {
            return new DoubleQuote();
        }
    }

    public abstract class QuotedString : Composite, GeneralString
    {
        private Token _quote;

        protected QuotedString(IEnumerable<Token> tokens)
        {
            _quote = tokens.First();
            _elements = tokens.ToArray();

            var content = _elements.Skip(1).Take(_elements.Length - 2);

            _text = content.Select(t => Convert(t).Value).ToSeparatorList("");
        }

        public static QuotedString Create(IEnumerable<Token> tokens)
        {
            var quote = tokens.First();
            if(quote is Base64Quote)
            {
                return new Base64SingleQuotedString(tokens);
            }
            else if (quote is SingleQuote)
            {
                return new SingleQuotedString(tokens);
            }
            else if (quote is DoubleQuote)
            {
                return new DoubleQuotedString(tokens);
            }
            else
            {
                throw new ArgumentException("expected quote, got " + quote + ".");
            }
        }

        protected virtual Token Convert(Token token)
        {
            if (token is EscapeSequence)
            {
                return ((EscapeSequence)token).Unescape();
            }
            else
            {
                return token;
            }
        }
    }

    public sealed class SingleQuotedString : QuotedString
    {
        public SingleQuotedString(IEnumerable<Token> tokens) : base(tokens) { }
    }

    public sealed class Base64SingleQuotedString : QuotedString
    {
        public Base64SingleQuotedString(IEnumerable<Token> tokens) : base(tokens) { }
    }

    public sealed class DoubleQuotedString : QuotedString
    {
        public DoubleQuotedString(IEnumerable<Token> tokens) : base(tokens) { }
    }

}
