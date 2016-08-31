
using Common.Tokens;
using System;
namespace Common.Tokenization.Locations
{
    /// <summary>
    /// Associated with tokens returned by the tokenizer.
    /// </summary>
    class ImmutableTokenLocation : ITokenLocation
    {
        public string BaseUri { get; private set; }

        public int StartPosition { get; private set; }

        public int EndPosition { get; private set; }

        public int Line { get; private set; }

        public int Column { get; private set; }

        public ImmutableTokenLocation(ITokenLocation location)
        {
            BaseUri = location.BaseUri;
            Line = location.Line;
            Column = location.Column;
            StartPosition = location.StartPosition;
            EndPosition = location.EndPosition;
        }

        public ImmutableTokenLocation(Token start, Token end)
        {
            if(null == start.Location || null == end.Location)
            {
                throw new ArgumentException("location information required.");
            }

            BaseUri = start.Location.BaseUri;
            Line = start.Location.Line;
            Column = start.Location.Column;
            StartPosition = start.Location.StartPosition;
            EndPosition = end.Location.EndPosition;
        }

        public override string ToString()
        {
            return BaseUri + "(" + (Line + 1) + "," + (Column + 1) + ")";
        }
    }
}
