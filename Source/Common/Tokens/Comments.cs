using System.Collections.Generic;
using System.Linq;

namespace Common.Tokens
{
    public abstract class CommentSeparator : Token
    {
    }

    public abstract class Comment : Composite
    {
    }

    #region C Style Comments

    public sealed class SingleLineCCommentInitiator : CommentSeparator
    {
        protected override void SetDefaultText()
        {
            _text = "//";
        }
    }

    public sealed class MultiLineCCommentInitiator : CommentSeparator
    {
        protected override void SetDefaultText()
        {
            _text = "/*";
        }
    }

    public sealed class MultiLineCCommentTerminator : CommentSeparator
    {
        protected override void SetDefaultText()
        {
            _text = "*/";
        }
    }

    public sealed class SingleLineCComment : Comment
    {
        public SingleLineCComment(IEnumerable<Token> tokens)
        {
            _elements = tokens.ToArray();
            _text = _elements.ToSeparatorList("");

            WithLocation(tokens.First());
        }
    }

    public sealed class MultiLineCComment : Comment
    {
        public MultiLineCComment(IEnumerable<Token> tokens)
        {
            _elements = tokens.ToArray();
            _text = _elements.ToSeparatorList("");

            WithLocation(tokens.First());
        }
    }

    #endregion
}
