
namespace Common.Tokenization
{
    /// <summary>
    /// Settings for the <see cref="TokenReader"/> class.
    /// </summary>
    public class TokenizerSettings
    {
        /// <summary>
        /// Omits white space, if set. The default is true.
        /// </summary>
        public bool OmitWhitespace { get; set; }

        public bool AcceptXml { get; set; }

        public bool FilterComments { get; set; }

        /// <summary>
        /// Optional default rule set for the tokenizer.
        /// </summary>
        public TokenizerRuleCollection DefaultRuleSet { get; set; }

        /// <summary>
        /// Constructs a settings object.
        /// </summary>
        public TokenizerSettings()
        {
            OmitWhitespace = true;
            AcceptXml = false;
        }

        /// <summary>
        /// Clones a settings object.
        /// </summary>
        /// <param name="existing">The settings to clone.</param>
        public TokenizerSettings(TokenizerSettings existing)
        {
            OmitWhitespace = existing.OmitWhitespace;
            FilterComments = existing.FilterComments;
        }
    }
}
