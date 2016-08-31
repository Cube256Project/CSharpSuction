using Common.Tokens;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Common.Tokenization
{
    /// <summary>
    /// Public extensions to the <see cref="TokenReader"/> class.
    /// </summary>
    public static class TokenizerExtensions
    {
        /// <summary>
        /// Reads tokens from a string.
        /// </summary>
        /// <param name="tokenizer">The tokenizer to use.</param>
        /// <param name="text">The text to parse.</param>
        /// <returns>Sequence of tokens.</returns>
        public static IEnumerable<Token> Read(this TokenReader tokenizer, string text)
        {
            return tokenizer.Read(new StringReader(text));
        }

    }
}
