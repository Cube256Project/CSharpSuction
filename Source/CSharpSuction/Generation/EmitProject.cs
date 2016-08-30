using Common;
using CSharpSuction.Input;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CSharpSuction.Generation
{
    /// <summary>
    /// Emits a MSBUILD project file for the suction result.
    /// </summary>
    public abstract class EmitProject : Emit
    {
        #region Protected

        protected class SourceImplictors
        {
            public HashSet<string> Namespaces = new HashSet<string>();
            public List<ClassDeclarationSyntax> Classes = new List<ClassDeclarationSyntax>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Related definitions can be found in Microsoft.Build.Commontypes.xsd (.NET Framework/.../MSBuild)
        /// </summary>
        public virtual string MSBuildNamespaceURI { get { return "http://schemas.microsoft.com/developer/msbuild/2003"; } }

        public virtual string ToolsVersion { get { return "4.0"; } }

        public string ProjectFile { get; private set; }

        public Guid ProjectGuid { get; private set; }

        public string SolutionFile { get; private set; }

        public string ProjectRootPrefix { get; private set; }

        #endregion

        #region Overrides

        protected override bool Generate()
        {
            ProjectGuid = Guid.NewGuid();
            ProjectFile = GetProjectFileName();

            using (var writer = XmlWriter.Create(ProjectFile, new XmlWriterSettings { Indent = true }))
            {
                // header
                EmitFirstPropertyGroup(writer);

                // references
                writer.WriteStartElement("ItemGroup");
                EmitReferences(writer);
                writer.WriteEndElement();

                // content
                writer.WriteStartElement("ItemGroup");
                foreach (var source in Suction.Sources.Where(IsSourceIncluded).OfType<SourceInfo>())
                {
                    EmitConvertedInputs(writer, source);
                    EmitDefault(writer, source);
                }
                writer.WriteEndElement();

                // assembly info
                writer.WriteStartElement("ItemGroup");
                EmitAssemblyInfo(writer);
                writer.WriteEndElement();

                // TODO: modify for other targets ...
                writer.WriteStartElement("Import");
                writer.WriteAttributeString("Project", @"$(MSBuildToolsPath)\Microsoft.CSharp.targets");
                writer.WriteEndElement();

            }

            Log.Information("project file '{0}' generated.", ProjectFile);

            OnAfterProjectFileGenerated();

            return true;
        }

        #endregion

        #region Protected Methods

        protected virtual string GetProjectFileName()
        {
            Directory.CreateDirectory(DestinationDirectory);
            return Path.Combine(DestinationDirectory, Suction.OutputName + ".csproj");
        }

        protected virtual void OnAfterProjectFileGenerated()
        {
        }

        protected virtual void EmitAssemblyInfo(XmlWriter writer)
        {
            var file = Path.Combine(DestinationDirectory, "AssemblyInfo." + Suction.OutputName + ".cs");

            var sb = new StringBuilder();

            var version = Suction.OutputVersion ?? new Version(0, 0, 0, 9999);

            sb.AppendLine("using System.Reflection;");

            sb.AppendLine("[assembly: AssemblyVersion(" + version.ToString().Quote(QuoteFormat.DoubleQuoteEscapeDuplicate) + ")]");

            File.WriteAllText(file, sb.ToString());

            writer.WriteStartElement("Compile");
            writer.WriteAttributeString("Include", MakeProjectRelativePath(file));
            writer.WriteEndElement();
        }

        protected virtual bool IsSourceIncluded(ISourceInfo source)
        {
            return source.State.IsIncluded();
        }

        protected virtual string MakeProjectRelativePath(string afile, string relativeto = null)
        {
            relativeto = relativeto ?? DestinationDirectory;
            if (afile.StartsWith(relativeto, StringComparison.InvariantCultureIgnoreCase))
            {
                // make relative path relative, this makes the project more 
                // structured in the VS solution explorer.
                afile = afile.Substring(relativeto.Length);
                afile = afile.TrimStart('/', '\\');
            }

            return afile;
        }

        private IEnumerable<string> Repeat(string s, int count)
        {
            for (int j = 0; j < count; ++j)
            {
                yield return s;
            }
        }

        protected virtual void EmitFirstPropertyGroup(XmlWriter writer)
        {
            var projectdir = Path.GetDirectoryName(ProjectFile);
            var destination = PathBuilder.Parse(MakeProjectRelativePath(projectdir, OutputBaseDirectory));
            var root = ProjectRootPrefix = Repeat("..", destination.Count).ToPath();

            var outputrelative = MakeProjectRelativePath(OutputDirectory, OutputBaseDirectory);

            if (string.IsNullOrEmpty(outputrelative))
            {
                outputrelative = "output";
                Log.Warning("project output defaults to {0}.", outputrelative.Quote());
            }

            var outputpath = Path.Combine(root, outputrelative);

            writer.WriteStartElement("Project", MSBuildNamespaceURI);
            writer.WriteAttributeString("ToolsVersion", ToolsVersion);

            writer.WriteStartElement("PropertyGroup");
            writer.WriteElementString("TargetFrameworkVersion", "v4.5");
            writer.WriteElementString("OutputPath", outputpath);
            writer.WriteElementString("DefineConstants", Suction.PreprocessorSymbols.ToSeparatorList(";"));
            writer.WriteElementString("ProjectGuid", ProjectGuid.ToString("B").ToUpper());

            // writer.WriteElementString("AssemblySearchPaths", "$(AssemblySearchPaths);" + Path.Combine(root, "Packages"));

            // [OutputType]


            writer.WriteElementString("OutputType", ConvertOutputKind(Suction.OutputKind));

            if (null != Suction.EntryPoint)
            {
                writer.WriteElementString("StartupObject", Suction.EntryPoint);
            }

            writer.WriteEndElement();
        }

        private string ConvertOutputKind(OutputKind kind)
        {
            switch (kind)
            {
                case OutputKind.ConsoleApplication:
                    return "Exe";

                case OutputKind.WindowsApplication:
                    return "Winexe";

                case OutputKind.DynamicallyLinkedLibrary:
                    return "Library";

                default:
                    Log.Warning("output kind {0} translates to 'exe'.", kind);
                    return "Exe";
            }
        }

        protected virtual string TranslatePath(ISourceInfo source)
        {
            return TranslatePath(source.FullPath);
        }

        protected string TranslatePath(string path)
        {
            return MakeProjectRelativePath(path);
        }

        protected void EmitDefault(XmlWriter writer, ISourceInfo source)
        {
            // translation/copy
            var path = TranslatePath(source);

            if (source is CompilationSourceInfo)
            {
                EmitCompile(writer, source, path);
            }
            else if (source is EmbeddedResourceSourceInfo)
            {
                EmitEmbeddedResource(writer, source, path);
            }
            else if (source is ConvertedInputInfo)
            {
                EmitConvertedInputs(writer, source, path);
            }
            else
            {
                throw new ArgumentException("unable to emit [" + source + "].");
            }
        }

        private void EmitDependentUpon(XmlWriter writer, ISourceInfo source)
        {
            foreach (var dependency in source.DependentUpon)
            {
                var path = TranslatePath(dependency);
                writer.WriteElementString("DependentUpon", MakeProjectRelativePath(path));
            }
        }

        private void EmitEmbeddedResource(XmlWriter writer, ISourceInfo source, string path)
        {
            writer.WriteStartElement("EmbeddedResource");
            writer.WriteAttributeString("Include", path);

            EmitDependentUpon(writer, source);

            writer.WriteEndElement();
        }

        private void EmitCompile(XmlWriter writer, ISourceInfo source, string path)
        {
            writer.WriteStartElement("Compile");
            writer.WriteAttributeString("Include", path);

            EmitDependentUpon(writer, source);

            writer.WriteEndElement();
        }

        private void EmitConvertedInputs(XmlWriter writer, ISourceInfo source, string path)
        {
            writer.WriteStartElement(source.Template);
            writer.WriteAttributeString("Include", path);
            writer.WriteElementString("Generator", "MSBuild:Compile");
            writer.WriteElementString("SubType", "Designer");
            writer.WriteEndElement();
        }

        protected void FindImplicators(SyntaxNode node, SourceImplictors to, bool classonly = false)
        {
            if (node is CompilationUnitSyntax)
            {
                foreach (var child in node.ChildNodes())
                {
                    FindImplicators(child, to);
                }
            }
            else if (!classonly && node is NamespaceDeclarationSyntax)
            {
                var ndecl = (NamespaceDeclarationSyntax)node;
                to.Namespaces.Add(ndecl.Name.ToString());

                foreach (var child in node.ChildNodes())
                {
                    FindImplicators(child, to, true);
                }
            }
            else if (node is ClassDeclarationSyntax)
            {
                to.Classes.Add((ClassDeclarationSyntax)node);
            }
        }

        protected void EmitSolutionFile()
        {
            var solutioname = Suction.OutputName + ".sln";

            // goes into output, not destination
            SolutionFile = Path.Combine(OutputBaseDirectory, solutioname);

            Guid left = Guid.NewGuid();
            Guid right = ProjectGuid;

            using (var writer = new StreamWriter(SolutionFile))
            {
                writer.NewLine = "\r\n";
                writer.WriteLine();
                writer.WriteLine("Microsoft Visual Studio Solution File, Format Version 12.00");
                writer.WriteLine("# Visual Studio 14");
                writer.WriteLine("VisualStudioVersion = 14.0.23107.0");
                writer.WriteLine("MinimumVisualStudioVersion = 10.0.40219.1");
                writer.Write("Project(\"{" + left.ToString().ToUpper() + "}\")");
                writer.Write(" = ");
                writer.Write(Suction.OutputName.QuoteHeavy());
                writer.Write(", ");

                var q = MakeProjectRelativePath(DestinationDirectory, OutputBaseDirectory);
                var projectpath = Path.Combine(q, Suction.OutputName + ".csproj");

                var rightq = right.ToString("B").ToUpper().QuoteHeavy();

                writer.Write(projectpath.QuoteHeavy());
                writer.Write(", ");
                writer.Write(rightq);

                writer.WriteLine();
                writer.WriteLine("EndProject");

                writer.WriteLine("Global");
                writer.WriteLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
                writer.WriteLine("\t\tDebug|Any CPU = Debug|Any CPU");
                writer.WriteLine("\tEndGlobalSection");
                writer.WriteLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
                writer.WriteLine("\t\t" + right.ToString("B").ToUpper() + ".Debug|Any CPU.ActiveCfg = Debug|x86");
                writer.WriteLine("\t\t" + right.ToString("B").ToUpper() + ".Debug|Any CPU.Build.0 = Debug|x86");
                writer.WriteLine("\tEndGlobalSection");
                writer.WriteLine("EndGlobal");
            }

            Log.Information("solution file '{0}' generated.", SolutionFile);

            EmitNuGet();

            var build = Path.Combine(OutputBaseDirectory, "build.cmd");
            using (var writer = new StreamWriter(build))
            {
                writer.WriteLine("@echo off");
                writer.WriteLine("nuget install");
                writer.WriteLine("msbuild /nologo /v:m");
            }
        }

        protected void EmitNuGet()
        {
            var nugetc = Path.Combine(OutputBaseDirectory, "NuGet.config");
            using (var writer = XmlWriter.Create(nugetc, new XmlWriterSettings { Indent = true }))
            {
                writer.WriteStartElement("configuration");
                writer.WriteStartElement("config");
                writer.WriteStartElement("add");
                writer.WriteAttributeString("key", "repositoryPath");
                writer.WriteAttributeString("value", "Packages");
            }

            var packages = Path.Combine(OutputBaseDirectory, "packages.config");
            using (var writer = XmlWriter.Create(packages, new XmlWriterSettings { Indent = true }))
            {
                writer.WriteStartElement("packages");

                var dict = new Dictionary<string, string>();

                foreach (var package in Suction.References.OfType<NuGetReferenceInfo>())
                {
                    // TODO: already?
                    dict[package.PackageName] = package.Version;
                }

                foreach (var e in dict)
                {
                    writer.WriteStartElement("package");
                    writer.WriteAttributeString("id", e.Key);
                    writer.WriteAttributeString("version", e.Value);
                    writer.WriteAttributeString("targetFramework", "net45");
                    writer.WriteEndElement();
                }
            }
        }

        #endregion

        #region Private Methods

        private void EmitReferences(XmlWriter writer)
        {
            /*foreach (var aref in suction.Compilation.References.OfType<PortableExecutableReference>())
            {
                writer.WriteStartElement("Reference");
                var path = aref.Display;
                writer.WriteAttributeString("Include", path);
                writer.WriteEndElement();
            }*/

            foreach (var r in Suction.References)
            {
                writer.WriteStartElement("Reference");

                if (r is SystemReferenceInfo)
                {
                    writer.WriteAttributeString("Include", ((SystemReferenceInfo)r).Location);
                }
                else if (r is RuntimeReferenceInfo)
                {
                    var x = (RuntimeReferenceInfo)r;

                    writer.WriteAttributeString("Include", x.Assembly.GetName().Name);
                }
                else if (r is NuGetReferenceInfo)
                {
                    var nu = (NuGetReferenceInfo)r;
                    writer.WriteAttributeString("Include", r.Location);

                    var hint = Path.Combine(ProjectRootPrefix, "Packages", nu.HintPath);

                    writer.WriteElementString("HintPath", hint);

                }
                else
                {
                    throw new NotImplementedException("unhandled [" + r.GetType().Name + "].");
                }

                writer.WriteEndElement();
            }

#if FOO
            var names = new string[]
            {
                "System",
                "System.Data",
                "System.Xml",
                "Microsoft.CSharp",
                "Microsoft.VisualStudio.QualityTools.UnitTestFramework",
                "System.Numerics",
                "System.Web",
                "WindowsBase",
                "PresentationCore",
                "PresentationFramework",
                "System.Xaml"
            };

            foreach (var name in names)
            {
                writer.WriteStartElement("Reference");
                writer.WriteAttributeString("Include", name);
                writer.WriteEndElement();
            }

            writer.WriteStartElement("Reference");
            writer.WriteAttributeString("Include", "Newtonsoft.Json");
            writer.WriteElementString("HintPath", "$(CFSThirdPartyBinaries)\\Newtonsoft.Json.dll");
            writer.WriteEndElement();
#endif
        }

        private void EmitConvertedInputs(XmlWriter writer, SourceInfo source)
        {
            foreach (var e in source.DependentUpon)
            {
                EmitDefault(writer, e);
            }
        }

        #endregion
    }
}
