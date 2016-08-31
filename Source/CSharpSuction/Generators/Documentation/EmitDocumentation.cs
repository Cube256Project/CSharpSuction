using System.Linq;
using System.Xml;
using CSharpSuction.Input;
using System.IO;
using CSharpSuction.Generators.Documentation.HTML;

namespace CSharpSuction.Generators.Documentation
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

            // step 1: convert suction result into XML ...
            var dom = new HtmlDocumentationBuilder().Build(Suction);

            // step 2: convert intermediate into HTML ...
            var htmlg = new HtmlDocumentationGenerator();
            htmlg.Results = Suction.Results;
            htmlg.OutputDirectory = DestinationDirectory;
            htmlg.SetParameters(Parameters);
            htmlg.Intermediate = dom;


            htmlg.Generate();

            return true;
        }
    }
}
