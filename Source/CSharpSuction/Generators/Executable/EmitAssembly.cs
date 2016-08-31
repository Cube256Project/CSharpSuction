using Microsoft.CodeAnalysis;
using System.IO;
using System.Linq;

namespace CSharpSuction.Generators.Executable
{
    /// <summary>
    /// Emits an assembly.
    /// </summary>
    public class EmitAssembly : Emit
    {
        /// <summary>
        /// Compiles the suction into an executable.
        /// </summary>
        /// <returns></returns>
        protected override bool Generate()
        {
            OutputDirectory = OutputDirectory ?? Suction.OutputDirectory;

            Directory.CreateDirectory(OutputDirectory);

            var ext = Suction.OutputKind == OutputKind.DynamicallyLinkedLibrary ? ".dll" : ".exe";
            var compilation = Suction.Compilation;

            Suction.Results.Debug("generate {0} trees to '{1}' ...", compilation.SyntaxTrees.Count(), OutputDirectory);

            var exclude = new string[] { "System.", "mscorlib.", "WindowsBase", "Presentation" };
            var include = new string[] { "System.Data.SqlServerCe" };

            foreach (var ar in compilation.References.OfType<MetadataReference>())
            {
                //Trace("assembly: {0}", ar.Display);
                //cmdwriter.WriteLine("/reference:" + ar.Display);
                var fullpath = ar.Display;
                var pname = Path.GetFileName(fullpath);
                if (!exclude.Any(f => pname.StartsWith(f)) || include.Any(f => pname.StartsWith(f)))
                {
                    var target = Path.Combine(OutputDirectory, pname);
                    Trace("copy assembly {0} ...", fullpath);
                    File.Copy(fullpath, target, true);
                }
            }

            foreach (var r in Suction.References)
            {

            }


            var filepath = Path.Combine(OutputDirectory, compilation.AssemblyName) + ext;
            var pdbpath = Path.Combine(OutputDirectory, compilation.AssemblyName) + ".pdb";

            var result = compilation.Emit(filepath, pdbpath);

            if (result.Success)
            {
                Trace("generated assembly '{0}' from {1} syntax tree(s).", compilation.AssemblyName, compilation.SyntaxTrees.Count());
                return true;
            }
            else
            {
                foreach (var diag in result.Diagnostics.Take(25))
                {
                    if (diag.Severity == DiagnosticSeverity.Error)
                    {
                        Trace("{0}", diag);
                    }
                }
                return false;
            }
        }
    }
}
