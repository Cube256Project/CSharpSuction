using Common.Tokenization.Attributes;
using System;
using System.Collections.Generic;

namespace Common.Tokens
{
    public abstract class Separator : Atomic
    {
        public abstract Composite CombineTokens(IEnumerable<Token> tokens);
    }

    [RegularExpression(@"\,")]
    public sealed class CommaSeparator : Atomic { }

    [RegularExpression(@"\.")]
    public sealed class DotSeparator : Separator
    {
        public override Composite CombineTokens(IEnumerable<Token> tokens)
        {
            return new DottedString(tokens);   
        }
    }

    [RegularExpression(@"\-")]
    public sealed class DashSeparator : Separator
    {
        public override Composite CombineTokens(IEnumerable<Token> tokens)
        {
            return new DashedString(tokens);
        }
    }

    public abstract class PathSeparator : Separator
    {
        public override Composite CombineTokens(IEnumerable<Token> tokens)
        {
            return new PathString(tokens);
        }
    }

    [RegularExpression(@"[\/]")]
    public sealed class ForwardSlash : PathSeparator
    {
    }

    [RegularExpression(@"[\\]")]
    public sealed class BackwardSlash : PathSeparator
    {
    }

    public abstract class ColonSeparator : Separator
    {
        public override Composite CombineTokens(IEnumerable<Token> tokens)
        {
            return new UrlString(tokens);
        }
    }


    [RegularExpression(@"\:")]
    public sealed class SimpleColonSeparator : ColonSeparator
    {
    }

    public sealed class UrlSeparator : ColonSeparator
    {
        protected override void SetDefaultText()
        {
            _text = "://";
        }
    }
}
