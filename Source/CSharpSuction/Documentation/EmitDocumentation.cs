using System.Linq;
using System.Xml;
using CSharpSuction.Input;
using System.IO;

namespace CSharpSuction.Documentation
{
    public class EmitDocumentation : Emit
    {
        #region Properties

        public XmlWriter Writer { get; private set; }

        public const string IntermediateNamespaceURI = "cfs:documentation-1591";
        public const string CodeDecorationNamespaceURI = "cfs:documentation-csharp-1596";

        #endregion

        protected override bool Generate()
        {
            var sources = Suction.Sources.Where(s => s.State == SourceState.Resolved).OfType<SourceInfo>();

            // convert suction result into XML ...
            var dom = new HtmlDocumentationBuilder().Build(Suction);

            // convert intermediate into HTML ...
            GenerateHtml(dom);

            return true;
        }

        #region Private Methods

        private void GenerateHtml(XmlDocument dom)
        {
            var htmlg = new HtmlDocumentationGenerator();
            htmlg.Results = Suction.Results;
            htmlg.OutputDirectory = DestinationDirectory;
            htmlg.Intermediate = dom;

            htmlg.Generate();
        }

        #endregion
    }
}
