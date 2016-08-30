using System;
using System.Linq;
using System.Text;

namespace CSharpSuction
{
    public class SummaryPrinter
    {
        public string PrintSummary(Suction suction, Func<ISourceInfo, bool> filter = null)
        {
            var output = new StringBuilder();

            output.AppendLine("source files:");
            foreach (var source in suction.Sources.Where(u => u.State.IsIncluded()))
            {
                output.AppendFormat("  {0,-20} {1}", source.State, source.FullPath);
                output.AppendLine();
            }

            output.AppendLine("types:");
            foreach (var type in suction.Types)
            {
                output.AppendFormat("  {0,-50} {1}", type.QualifiedName, type.Sources.Select(s => s.FullPath).ToSeparatorList());
                output.AppendLine();
            }

            return output.ToString();
        }
    }
}
