using Common;
using CSharpSuction.Configuration;
using CSharpSuction.Generators.Executable;
using CSharpSuction.Generators.Project;
using CSharpSuction.Input;
using CSharpSuction.Toolmap;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows;
using System.Windows.Markup;

namespace CSharpSuction
{
    /// <summary>
    /// Console utility program for <see cref="Suction"/>.
    /// </summary>
    /// <remarks>
    /// <para>Runs the <see cref="Suction"/> process based on a configuration file (.suction.xml)
    /// and/or command line parameters.</para>
    /// <para>The utility can also be used as a library, by calling the <see cref="Run(string[])"/> method; 
    /// the call must occur on a <see cref="STAThreadAttribute"/> thread.</para>
    /// </remarks>
    public class Utility
    {
        #region Private

        private Suction _suction;
        private ProjectDescriptor _project;
        private HashSet<string> _postprocessed = new HashSet<string>();
        private IToolmap _toolmap;
        private HashSet<string> _namespacesused = new HashSet<string>();
        private EmitInstruction _emitoverride = null;

        #endregion

        #region Properties

        internal static int Indent { get; set; }

        internal string TemporaryDirectory { get; private set; }

        public bool ShowSummary { get; set; }

        public List<string> SourceDirectories = new List<string>();

        public string OutputBaseDirectory { get; private set; }

        #endregion

        #region Diagnostics

        internal void Trace(string format, params object[] args)
        {
            if (null == _suction)
            {
                Log.Debug(format, args);
            }
            else
            {
                _suction.Results.Debug(format, args);
            }
        }

        #endregion

        #region EntryPoint

        [STAThread]
        static int Main(string[] args)
        {
            LogContext.Default.AddFollower(new ConsoleLogFollower());
            Log.DefaultMinimumSeverity = LogSeverity.debug;

            return new Utility().Run(args);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Runs the command with the given arguments.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public int Run(params string[] args)
        {
            try
            {
                TemporaryDirectory = Directory.GetCurrentDirectory();

                CreateSuction();

                Trace("CSharpSuction, v{0}", GetType().Assembly.GetName().Version.ToString());
                Trace("");

                TryLoadAdditionalGenerators();

                // create project descriptor before arguments are parsed
                _project = new ProjectDescriptor();

                // this will parse the project ...
                ParseArguments(args);

                if (!_project.IsLoaded)
                {
                    // project not specified
                    PrintSyntax();
                    return 1;
                }

                // setup compilation DEFINEs
                foreach (var pps in _project.Defines)
                {
                    _suction.PreprocessorSymbols.Add(pps.Key);
                }

                // TODO: not good
                Trace("add default assemblies ...");
                _suction.AddAssemblyReference(typeof(object).Assembly);
                _suction.AddAssemblyReference(typeof(Enumerable).Assembly);
                _suction.AddAssemblyReference(typeof(Regex).Assembly);

                _suction.AddAssemblyReference(typeof(IQueryAmbient).Assembly);
                _suction.AddAssemblyReference(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).Assembly);

                var dummy = new Window();
                var dummz = new DataTable();


                Trace("processing ...");

                // apply projects settings to the suction
                var names = new ProjectApplicator().ApplyProject(_suction, _project);

                // expand the names (resolve symbol references)
                if (!_suction.Expand(names))
                {
                    throw new Exception("failed to expand names.");
                }

                if (ShowSummary)
                {
                    Trace("{0}", new SummaryPrinter().PrintSummary(_suction));
                }

                if (!_suction.Types.Any())
                {
                    Log.Warning("no matching types were found.");
                }

                // emit
                if (null != _emitoverride)
                {
                    _project.Emits.Clear();
                    _project.Emits.Add(_emitoverride);
                }
                else if(!_project.Emits.Any())
                {
                    CreateDefaultEmit();
                }

                foreach (var emiting in _project.Emits)
                {
                    // create the emitter object
                    var emit = CreateEmit(emiting);

                    emit.OriginalDirectory = _project.OriginalDirectory;

                    Trace("using [{0}] to emit.", emit.GetType().Name);

                    Trace("extracted {0} files from {1} including {2} types.",
                        _suction.Sources.Where(u => u.State == SourceState.Resolved).Count(),
                        _suction.Sources.Count(),
                        _suction.Types.Count()
                        );

                    if (null == OutputBaseDirectory)
                    {
                        OutputBaseDirectory = Directory.GetCurrentDirectory();
                    }

                    Trace("output directory {0} ...", OutputBaseDirectory.Quote());

                    // assign emit parameters
                    emit.OutputBaseDirectory = OutputBaseDirectory;
                    emit.OutputDirectory = _project.OutputDirectory;
                    emit.DestinationDirectory = emiting.Destination;
                    emit.Parameters = emiting.Parameters;

                    Trace("emit [{0}] to output directory '{1}'.", emit.GetType().Name, emit.OutputDirectory);

                    // perform emit
                    emit.Generate(_suction);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Log.Create()
                    .WithHeader("error: {0}", ex.Message)
                    .WithSeverity(LogSeverity.error)
                    .Submit();

                return 3;
            }
        }

        private void CreateDefaultEmit()
        {
            _project.Emits.Add(new EmitInstruction(typeof(EmitAssembly)));
        }

        private void CreateSuction()
        {
            _suction = new Suction();
            _suction.ShowVerbose = true;

            // register suction handlers ...
            _suction.OnCompilationScheduled += Suction_OnCompilationScheduled;
            _suction.OnSourceLoaded += Suction_OnSourceLoaded;
            _suction.OnResolverCreated += Suction_OnResolverCreated;
            _suction.OnUnprocessedFile += Suction_OnUnprocessedFile;
            _suction.OnUsing += Suction_OnUsing;
        }

        private void PrintSyntax()
        {
            Trace("syntax: suction [ -vspo ] <project-file> [ -g { goal } ]\n");
            Trace("  -v verbose output.");
            Trace("  -s print summary.");
            Trace("  -g specify goals.");
            Trace("  -i specify input path.");
            Trace("  -o creates project file on original sources.");
            Trace("  -p creates project file and copies sources.");
            Trace("  -e <path> override output directory.");
            Trace("");
        }

        #endregion

        #region Private Methods

        private void TryLoadAdditionalGenerators()
        {
            var names = new string[] { "Suction.TypeScriptGenerator", "Suction.JavaScriptGenerator" };

            foreach (var name in names)
            {
                try
                {
                    Assembly.Load(name);
                }
                catch (Exception)
                {
                    Log.Trace("generator assembly {0} not loaded.", name);
                }
            }
        }

        private Emit CreateEmit(EmitInstruction emit)
        {
            return (Emit)Activator.CreateInstance(emit.EmitterType);
        }

        private void ParseArguments(string[] args)
        {
            int s = 0;
            string projectfile = null;
            for (int j = 0; j < args.Length; ++j)
            {
                var arg = args[j];
                if (s == 0)
                {
                    if (arg.StartsWith("-"))
                    {
                        switch (arg)
                        {
                            case "-v":
                                _suction.ShowResolve = true;
                                break;

                            case "-s":
                                ShowSummary = true;
                                break;

                            case "-p":
                                _emitoverride = new EmitInstruction(typeof(EmitProjectCopy));
                                break;

                            case "-o":
                                _emitoverride = new EmitInstruction(typeof(EmitProjectOnOriginalSource));
                                break;

                            case "-i":
                                s = 3;
                                break;

                            case "-e":
                            case "--output-directory":
                                s = 1;
                                break;

                            case "-g":
                                s = 2;
                                break;

                            default:
                                throw new Exception("unhandled option '" + arg + ".");
                        }
                    }
                    else
                    {
                        if (null != projectfile)
                        {
                            throw new Exception("only one project file argument allowed.");
                        }

                        projectfile = CheckForProjectFile(arg);
                    }
                }
                else if (1 == s)
                {
                    var path = arg;
                    if (!Path.IsPathRooted(path))
                    {
                        path = Path.Combine(Directory.GetCurrentDirectory(), path);
                    }

                    path = Path.GetFullPath(path);

                    OutputBaseDirectory = path;
                    s = 0;
                }
                else if (2 == s)
                {
                    if (arg.StartsWith("-"))
                    {
                        // reparse
                        --j;
                        s = 0;
                    }
                    else
                    {
                        _project.Goals.Add(arg);
                    }
                }
                else if (3 == s)
                {
                    SourceDirectories.Add(arg.MakeAbsolutePath());
                    s = 0;
                }
            }

            if (null != projectfile)
            {
                Trace("loading '{0}' ...", projectfile);
                _project.Load(projectfile);
            }
            else if (_project.Goals.Any())
            {
                Trace("goals were specified, no project file ...");

                if(!SourceDirectories.Any())
                {
                    Trace("using current directory.");
                    SourceDirectories.Add(Directory.GetCurrentDirectory());
                }

                foreach (var source in SourceDirectories)
                {
                    _project.AddSourceDirectory(source);
                }

                _project.IsLoaded = true;
            }

        }

        private string CheckForProjectFile(string path)
        {
            if (!Path.IsPathRooted(path))
            {
                if (!File.Exists(path))
                {
                    var longpath = path + ".suction.xml";
                    if (File.Exists(longpath))
                    {
                        path = longpath;
                    }
                }

                path = Path.GetFullPath(path);
            }

            return path;
        }

        private void Suction_OnResolverCreated(object sender, SuctionEventArgs e)
        {
            var suction = (Suction)sender;
            var path = e.FullPath;
            if (path.EndsWith(".xaml.cs"))
            {
                if (!_postprocessed.Contains(path))
                {
                    // XAML file present?
                    var xamlfile = path.Substring(0, path.Length - 3);
                    if (File.Exists(xamlfile))
                    {
                        // load XAML ...
                        /*var c = new XamlConverter(suction, xamlfile);
                        c.OutputDirectory = TemporaryDirectory;
                        var source = c.Process1();

                        source.AddPostProcessing(q => c.Process2(q));
                        source.Schedule();*/

                        e.Source.AddDependentUpon(new ConvertedInputInfo(suction, xamlfile));

                        // suction.AddSourceTree(tree).Schedule();
                    }

                    _postprocessed.Add(path);
                }
            }
        }

        private void Suction_OnSourceLoaded(object sender, SuctionEventArgs e)
        {
        }

        private void Suction_OnCompilationScheduled(object sender, SuctionEventArgs e)
        {
        }

        private void Suction_OnDisambiguateMapping(object sender, SuctionEventArgs e)
        {
            var ifname = e.Mapping.Select(m => m.InterfaceTypeName).First();

            Trace("select implementation for interface [{0}]:", ifname);
            foreach (var option in e.Mapping.Select(m => m.ImplementationTypeName))
            {
                Trace("  {0}", option);
            }

            if (ifname == "IGRA3.HCD.IHCDClientFactory")
            {
                e.Mapping = e.Mapping.Where(m => m.ImplementationTypeName == "IGRA3.HCD.Client.HttpClientFactory");
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void Suction_OnUsing(object sender, SuctionEventArgs e)
        {
            if(null == _toolmap)
            {
                using (var toolmapstream = GetType().Assembly.GetResourceStream("Toolmap.xml"))
                {
                    _toolmap = new ToolmapReader().Load(toolmapstream);
                }
            }

            var suction = (Suction)sender;
            if (e.Code == SuctionEventCode.IncludeUsing)
            {
                var sarg = e.Name;

                if (_namespacesused.Add(sarg))
                {
                    foreach (var location in new UsingImplicator(_toolmap).GetAssemblyLocations(sarg))
                    {
                        suction.AddAssemblyReference(location);
                    }
                }
            }
        }

        private void Suction_OnUnprocessedFile(object sender, SuctionEventArgs e)
        {
            var ext = Path.GetExtension(e.FullPath).ToLower();
            if (_project.ResourceExtensions.Contains(ext))
            {
                Log.Trace("adding extension file '{0}' ...", e.FullPath);

                e.Source = _suction.AddEmbeddedResource(e.FullPath);
                e.Handled = true;
            }
        }

        #endregion
    }
}
