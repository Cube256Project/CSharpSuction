
using Common.Tokenization.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace Common.Tokens
{
    [RegularExpression("[A-Za-z_][A-Za-z0-9_]*")]
    public sealed class Identifier : Atomic, GeneralString, XmlSimpleName
    {
        public Identifier() { }

        public Identifier(string value)
        {
            _text = value;
        }
    }

    /// <summary>
    /// CFSL asset identifier; '$' followed by a path string.
    /// </summary>
    public sealed class AssetIdentifierToken : Composite
    {
        public AssetIdentifierToken(Dollar start)
            : base(new[] { start })
        {
        }

        internal void Extend(IEnumerable<Token> tokens)
        {
            SetElements(Elements.Concat(tokens));
        }
    }

    /// <summary>
    /// SQL style variable; '@' followed by an identifier.
    /// </summary>
    public sealed class VariableIdentifierToken : Composite
    {
        public VariableIdentifierToken(IEnumerable<Token> tokens)
            : base(tokens)
        { }
    }

}
