using System.Security.Cryptography;
using Common;

namespace CSharpSuction.Generators.Documentation.HTML
{
    /// <summary>
    /// Handles callbacks from the <see cref="IntermediateToHTML"/> transform.
    /// </summary>
    class HtmlDocumentationCallback
    {
        private HtmlDocumentationGenerator _gen;

        public HtmlDocumentationCallback(HtmlDocumentationGenerator gen)
        {
            _gen = gen;
        }

        public string TranslateReference(string cref)
        {
            return _gen.TranslateReference(cref);
        }

        public string GenerateRandomID()
        {
            var bytes = new byte[8];
            RandomNumberGenerator.Create().GetBytes(bytes);
            return bytes.ToBase32();
        }

        public string GetParameter(string arg)
        {
            string result;
            _gen.Parameters.TryGetValue(arg, out result);
            return result ?? "[undefined-parameter: " + arg + "]";
        }
    }
}
