using Common;
using CSharpSuction.Input;
using CSharpSuction.Resolution;
using CSharpSuction.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace CSharpSuction
{
    /// <summary>
    /// Extracts sources from a collection of C# source files using Roslyn.
    /// </summary>
    public class Suction
    {
        #region Private

        private List<SyntaxTree> _originaltrees = new List<SyntaxTree>();

        /// <summary>
        /// Set of all sources, mapped by their fullpath.
        /// </summary>
        /// <remarks>TODO: This includes resource files?</remarks>
        private Dictionary<string, SourceInfo> _sources = new Dictionary<string, SourceInfo>();

        /// <summary>
        /// Mapps syntax trees to source infos.
        /// </summary>
        private Dictionary<SyntaxTree, SourceInfo> _trees = new Dictionary<SyntaxTree, SourceInfo>();

        private SortedList<string, TypeInfo> _typemap = new SortedList<string, TypeInfo>();

        private Dictionary<string, AssemblyReferenceInfo> _references = new Dictionary<string, AssemblyReferenceInfo>();

        private List<string> _assemblyreferences = new List<string>();
        private Dictionary<string, MetadataReference> _metadatareferences = new Dictionary<string, MetadataReference>();
        private HashSet<string> _unresolvednames = new HashSet<string>();
        private TreeCollection _completedtrees = new TreeCollection();
        private NameMap _names = new NameMap();
        private Dictionary<string, List<string>> _interfaces = new Dictionary<string, List<string>>();
        private CSharpCompilation _compilation = null;
        private HashSet<string> _checkedinterfacenames = new HashSet<string>();
        private FactorySelection _selection = new FactorySelection();
        private CSharpCompilationOptions _options;
        private List<ClassDeclarationSyntax> _initonboot = new List<ClassDeclarationSyntax>();
        private HashSet<string> _extractedusings = new HashSet<string>();
        private HashSet<string> _includedusings = new HashSet<string>();

        private List<string> _assemblysearchpath = new List<string>();
        private List<string> _preprocessorsymbols = new List<string>();

        private Dictionary<string, SourceInfo> _compilationsources = new Dictionary<string, SourceInfo>();
        private List<SourceInfo> _compilationqueue = new List<SourceInfo>();
        private Queue<Resolver> _resolverqueue = new Queue<Resolver>();
        private Queue<Resolver> _redoqueue = new Queue<Resolver>();

        #endregion

        #region Diagnostics

        public bool ShowVerbose = false;
        public bool ShowNames = false;
        public bool ShowResolve = false;

        public void Trace(string format, params object[] args)
        {
            Results.Debug(format, args);

            if (null != OnMessage)
            {
                var e = new SuctionEventArgs();
                e.Code = SuctionEventCode.Message;
                e.Message = string.Format(format, args);

                OnMessage(this, e);
            }
        }

        public void TraceVerbose(string format, params object[] args)
        {
            if (ShowVerbose)
            {
                Trace(format, args);
            }
        }

        public void TraceResolve(string format, params object[] args)
        {
            if (ShowResolve)
            {
                Trace(format, args);
            }
        }

        public void TraceName(string format, params object[] args)
        {
            if (ShowNames)
            {
                Trace(format, args);
            }
        }

        public void TraceUnresolved(string format, params object[] args)
        {
        }

        public void TraceCompilation(string format, params object[] args)
        {
        }

        #endregion

        #region Properties

        public IList<string> AssemblySearchPaths { get { return _assemblysearchpath; } }

        public IList<string> PreprocessorSymbols { get { return _preprocessorsymbols; } }

        public OutputKind OutputKind { get; set; }

        /// <summary>
        /// The name of the output object, meaning is emitter dependent.
        /// </summary>
        public string OutputName { get; set; }

        public string OutputDirectory { get; set; }

        public Version OutputVersion { get; set; }

        /// <summary>
        /// The name of the class containing the entry point.
        /// </summary>
        public string EntryPoint { get; set; }

        public CSharpCompilation Compilation { get { return _compilation; } }

        public FactorySelection Selection { get { return _selection; } }

        public IEnumerable<SyntaxTree> OriginalTrees { get { return _originaltrees; } }

        /// <summary>
        /// The collection of sources of this suction.
        /// </summary>
        /// <remarks>This includes all source files included in the process.</remarks>
        public IEnumerable<ISourceInfo> Sources { get { return _sources.Values; } }

        /// <summary>
        /// Set set of extract types.
        /// </summary>
        public IEnumerable<ITypeInfo> Types { get { return _typemap.Values; } }

        public bool EnableObjectFactory { get; set; }

        public IEnumerable<AssemblyReferenceInfo> References { get { return _references.Values; } }

        /// <summary>
        /// Receives processing results (log, warnings).
        /// </summary>
        public ResultWriter Results { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// Triggered when a new namespace using statement is found.
        /// </summary>
        public event SuctionEventHandler OnUsing;
        public event SuctionEventHandler OnDisambiguateMapping;
        public event SuctionEventHandler OnCompilationError;
        public event SuctionEventHandler OnMessage;
        public event SuctionEventHandler OnCompilationScheduled;
        public event SuctionEventHandler OnSourceLoaded;
        public event SuctionEventHandler OnResolverCreated;

        /// <summary>
        /// Triggered when an unprocessed file is encountered.
        /// </summary>
        public event SuctionEventHandler OnUnprocessedFile;

        #endregion

        #region Construction

        /// <summary>
        /// Constructs a new suction object.
        /// </summary>
        public Suction()
        {
            OutputKind = OutputKind.ConsoleApplication;
            EnableObjectFactory = true;

            // install default result writer
            Results = new ResultWriter();
        }

        #endregion

        #region Public Methods

        public void AddAssemblyReference(Assembly a)
        {
            AddAssemblyReference(new RuntimeReferenceInfo(a));
        }

        /// <summary>
        /// Adds an assembly reference info to the suction.
        /// </summary>
        /// <param name="info"></param>
        public void AddAssemblyReference(AssemblyReferenceInfo info)
        {
            if (!_references.ContainsKey(info.Key))
            {
                _references.Add(info.Key, info);

                var filepath = info.GetFullPath();


                // add metadata reference.
                if (!_metadatareferences.ContainsKey(filepath))
                {
                    //TraceResolve("adding assembly reference '{0}' ...", filepath);

                    Log.Trace("reference {0} ...", info.Key);

                    _metadatareferences.Add(filepath, MetadataReference.CreateFromFile(filepath));
                    _assemblyreferences.Add(filepath);

                    if (null != _compilation)
                    {
                        _compilation = _compilation.WithReferences(_metadatareferences.Values);
                    }
                }
            }
#if FOO
            var filepath = string.Intern(info.Location);

            var match = false;
            if (!Path.IsPathRooted(filepath))
            {
                if (!_referencepath.Any())
                {
                    throw new Exception("relative reference name '" + filepath + "' specified but no reference path was set.");
                }
                // look in reference path
                foreach (var rp in _referencepath)
                {
                    var path = Path.Combine(rp, filepath);
                    if (File.Exists(path))
                    {
                        filepath = path;
                        match = true;
                        break;
                    }
                }
            }
            else
            {
                match = true;
            }

            if (!match)
            {
                throw new Exception("assembly reference '" + filepath + "' was not resolved.");
            }

            if (!_assemblyreferences.Contains(filepath))
            {
                TraceResolve("adding assembly reference '{0}' ...", filepath);

                _metadatareferences.Add(MetadataReference.CreateFromFile(filepath));
                _assemblyreferences.Add(filepath);

                if (null != _compilation)
                {
                    _compilation = _compilation.WithReferences(_metadatareferences);
                }
            }
#endif
        }

        /// <summary>
        /// Scans sources from a directory.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="filter">Regular expression filtering the files.</param>
        public void AddSourceDirectory(string directory, Regex filter = null)
        {
            // recurse
            ScanSourceDirectory(directory, true, filter);
        }

        /// <summary>
        /// Adds a single source file.
        /// </summary>
        /// <param name="sourcefile">The full path of the source file.</param>
        public ISourceInfo AddSourceFile(string sourcefile)
        {
            var ext = Path.GetExtension(sourcefile).ToLower();

            if (ext == ".cs")
            {
                var newsource = new CompilationSourceInfo(this, sourcefile);

                // TODO: handle source updates!
                AddOrReplaceSource(newsource);

                // read the source file ...
                using (var reader = new StreamReader(sourcefile))
                {
                    LoadSourceCode(newsource, reader.ReadToEnd());
                }

                return newsource;
            }
            else if (ext == ".csproj")
            {
                // TODO: ignore project files for the time being; might want to process.
                return null;
            }
            else
            {
                var e = new SuctionEventArgs();
                e.Code = SuctionEventCode.UnprocessedFile;
                e.FullPath = sourcefile;

                if (null != OnUnprocessedFile)
                {
                    OnUnprocessedFile(this, e);
                }

                if (null == e.Source)
                {
                    if (!e.Handled)
                    {
                        Results.Write(new UnprocessedFileWarning(sourcefile));
                    }

                    return null;
                }
                else
                {
                    return e.Source;
                }
            }
        }

        /// <summary>
        /// Adds a <see cref="SyntaxTree"/> to the suction.
        /// </summary>
        /// <param name="tree">The syntax tree to add.</param>
        /// <returns>The source info.</returns>
        public ISourceInfo AddSourceTree(SyntaxTree tree)
        {
            var newsource = new CompilationSourceInfo(this, tree.FilePath);

            AddOrReplaceSource(newsource);

            using (var reader = new StreamReader(tree.FilePath))
            {
                LoadSourceCode(newsource, reader.ReadToEnd());
            }

            return newsource;
        }

        public ISourceInfo AddEmbeddedResource(string sourcefile)
        {
            var info = new EmbeddedResourceSourceInfo(this, sourcefile);

            AddOrReplaceSource(info);

            return info;
        }

        private Regex SetupFilter(string filter)
        {
            return WildcardFactory.BuildWildcardsFromList(filter ?? "*.cs");
        }

        /// <summary>
        /// Adds source files to the suction.
        /// </summary>
        /// <param name="fileordirectory">A file or directory path.</param>
        /// <param name="filter">A ';' separated list of wildcard expressions, as <see cref="WildcardFactory"/>.</param>
        public void AddSource(string fileordirectory, string filter = null)
        {
            var rx = SetupFilter(filter);

            if (Directory.Exists(fileordirectory))
            {
                AddSourceDirectory(fileordirectory, rx);
            }
            else if (File.Exists(fileordirectory))
            {
                if (rx.IsMatch(fileordirectory))
                {
                    AddSourceFile(fileordirectory);
                }
                else
                {
                    throw new Exception("unable to include '" + fileordirectory + "', extension not handled.");
                }
            }
            else
            {
                throw new Exception("unable to include '" + fileordirectory + "', extension not handled, file not found.");
            }
        }

        /// <summary>
        /// Returns a collection of syntax nodes corresponding to a declared name.
        /// </summary>
        /// <param name="name">
        /// The declaration name to search. 
        /// This can include multiple wildcards, see <see cref="WildcardFactory"/>.
        /// </param>
        /// <returns>All matching names.</returns>
        public IEnumerable<INameInfo> LookupName(string fullname)
        {
            var parts = fullname.Split('.');
            var name = parts.Last();
            var isqualified = parts.Length > 1;

            var nameinfos = _names.LookupName(name);
            if (isqualified)
            {
                var result = new List<NameInfo>();
                foreach (var info in nameinfos)
                {
                    if (info.Node.GetDeclaration() == fullname)
                    {
                        result.Add(info);
                    }
                }

                nameinfos = result;
            }

            return nameinfos;
        }

        /// <summary>
        /// Looks for unresolved symbols in a set of syntax trees and adds source files.
        /// </summary>
        /// <param name="results">Collection of name infos to include (goals).</param>
        public bool Expand(IEnumerable<INameInfo> results)
        {
            // schedule specified sources ...
            foreach (var source in results.Select(r => r.Source))
            {
                TraceResolve("initial '{0}' ...", source.FullPath);
                source.Schedule();
            }

            int initializer = 0;

            // main loop
            while (true)
            {
                // add any new sources to the compilation
                if (ExtendCompilation())
                {
                    continue;
                }

                // Log.Debug("\n\t*** PASS ***\n");

                // empty the resolver queue
                if (_resolverqueue.Any())
                {
                    while (_resolverqueue.Any())
                    {
                        var resolver = _resolverqueue.Dequeue();

                        TraceResolve("resolve source '{0}' ...", resolver.Source.FullPath);
                        resolver.Resolve();

                        if (!resolver.IsCompleted)
                        {
                            _redoqueue.Enqueue(resolver);
                        }
                    }

                    continue;
                }

                // requeue redoing resolvers
                if (_redoqueue.Any())
                {
                    while (_redoqueue.Any())
                    {
                        _resolverqueue.Enqueue(_redoqueue.Dequeue());
                    }

                    continue;
                }

                if (EnableObjectFactory)
                {
                    // object factory interfaces referenced
                    if (ProcessObjectFactoryAttributes())
                    {
                        continue;
                    }

                    if (ApplySelection())
                    {
                        continue;
                    }

                    if (0 == initializer)
                    {
                        initializer = 1;
                        CreateInitializer();
                        continue;
                    }
                }

                break;
            }

            // Log.Debug("\n\t*** CHECK ***\n");

            var unresolved = _sources.Values.Where(s => s.State == SourceState.Unresolved);
            if (unresolved.Any())
            {
                foreach (var u in unresolved)
                {
                    Log.Warning("{0} has unresolved items.", u.FullPath);
                }

                return true;
            }
            else
            {
                return true;
            }
        }

        public void RefreshSourceDirectory(string directory)
        {
            ScanSourceDirectory(directory, false);
        }

        #endregion

        #region Internal Methods

        internal void AddTypeSource(INamedTypeSymbol symbol, SourceInfo source, SyntaxNode declaration)
        {
            TypeInfo info;
            var lookup = new TypeInfo(symbol);
            if (!_typemap.TryGetValue(lookup.QualifiedName, out info))
            {
                _typemap[lookup.QualifiedName] = info = lookup;
            }

            info.Add(source);
            info.Add(declaration);
        }

        #endregion

        #region Private Methods

        #region Source Management

        private bool IsFileIncluded(string path)
        {
            var name = Path.GetFileName(path).ToLower();

            if (name.EndsWith(".g.cs") || name.EndsWith(".g.i.cs") || name == "AssemblyInfo.cs")
            {
                return false;
            }

            switch (Path.GetExtension(name))
            {
                case ".dll":
                case ".pdb":
                case ".exe":
                    return false;
            }

            return true;
        }

        private bool IsDirectoryIncluded(string directoryname)
        {
            var name = Path.GetFileName(directoryname).ToLower();

            switch (name)
            {
                case "bin":
                case "obj":
                case "tmp":

                // TODO: this is somewhat risky ...
                case "output":
                    return false;

                default:
                    return true;
            }
        }

        private void ScanSourceDirectory(string parent, bool recurse, Regex filter = null)
        {
            if (Directory.Exists(parent))
            {
                foreach (var file in Directory.GetFiles(parent))
                {
                    if (IsFileIncluded(file))
                    {
                        if (null == filter || filter.IsMatch(file))
                        {
                            AddSourceFile(file);
                        }
                        else
                        {
                            Log.Trace("[ScanSourceDirectory] ignored file {0}.", file.Quote());
                        }
                    }
                }

                if (recurse)
                {
                    foreach (var child in Directory.GetDirectories(parent))
                    {
                        if (IsDirectoryIncluded(child))
                        {
                            ScanSourceDirectory(child, true, filter);
                        }
                    }
                }
            }
        }

        private void LoadSourceCode(SourceInfo newsource, string text)
        {
            var options = new CSharpParseOptions(LanguageVersion.CSharp6);
            //options = options.WithPreprocessorSymbols("DEBUG", "TRACE");

            options = options.WithPreprocessorSymbols(PreprocessorSymbols);

            var tree = CSharpSyntaxTree.ParseText(text, options, newsource.FullPath, Encoding.UTF8);

            LoadSourceTree(newsource, tree);
        }

        private void AddOrReplaceSource(SourceInfo newsource)
        {
            var sourcefile = newsource.FullPath;
            SourceInfo existing;
            if (_sources.TryGetValue(sourcefile, out existing))
            {
                Trace("replace source '{0}' ...", newsource.FullPath);

                _compilation = _compilation.RemoveSyntaxTrees(existing.Tree);
                _sources.Remove(existing.FullPath);

                _sources.Add(newsource.FullPath, newsource);
            }
            else
            {
                // add a new source file to the sources collection.
                _sources.Add(newsource.FullPath, newsource);
            }
        }

        private void LoadSourceTree(SourceInfo newsource, SyntaxTree tree)
        {
            _originaltrees.Add(tree);

            // map syntax tree to the new source
            _trees[tree] = newsource;

            // change to the extracted state
            newsource.Tree = tree;

            // extract names from this source
            ExtractNames(newsource);

            // TraceName("loaded source '{0}'.", tree.FilePath);

            // trigger event
            if (null != OnSourceLoaded)
            {
                var e = new SuctionEventArgs();
                e.Code = SuctionEventCode.SourceLoaded;
                e.Source = newsource;
                e.FullPath = newsource.FullPath;

                OnSourceLoaded(this, e);
            }
        }

        #endregion

        #region Compilation

        private void CreateCompilation()
        {
            _options = new CSharpCompilationOptions(OutputKind);

            if (null != EntryPoint)
            {
                _options = _options.WithMainTypeName(EntryPoint);
            }
            else
            {
                _options = _options.WithOutputKind(OutputKind);
            }

            if (null == OutputName)
            {
                OutputName = "suction-output";

                Trace("using default output name '{0}'.", OutputName);
            }

            _compilation = CSharpCompilation.Create(OutputName,
                references: _metadatareferences.Values,
                options: _options);
        }

        /// <summary>
        /// Schedules a source for compilation in the next round.
        /// </summary>
        /// <param name="source"></param>
        internal void ScheduleCompilation(ISourceInfo source)
        {
            if (!_compilationsources.ContainsKey(source.FullPath))
            {
                // Log.Debug("schedule compilation '{0}' ...", source.FullPath);

                _compilationqueue.Add((SourceInfo)source);
                _compilationsources.Add(source.FullPath, (SourceInfo)source);

                if (null != OnCompilationScheduled)
                {
                    var e = new SuctionEventArgs();
                    e.FullPath = source.FullPath;
                    OnCompilationScheduled(this, e);
                }
            }
            else
            {
                Log.Warning("duplicate scheduling of {0}.", source.FullPath.Quote());
            }
        }

        internal bool ExtendCompilation()
        {
            if (null == _compilation)
            {
                CreateCompilation();
            }

            if (_compilationqueue.Any())
            {
                while (_compilationqueue.Any())
                {
                    var newsources = _compilationqueue.ToList();
                    _compilationqueue.Clear();

                    var trees = newsources.Select(e => e.Tree);

                    _compilation = _compilation.AddSyntaxTrees(trees);

                    foreach (var source in newsources)
                    {
                        // create resolver for the new source
                        var resolver = new Resolver(this, source);

                        if (null != OnResolverCreated)
                        {
                            var e = new SuctionEventArgs();
                            e.Code = SuctionEventCode.ResolverCreated;
                            e.Source = resolver.Source;
                            e.FullPath = resolver.Source.FullPath;
                            OnResolverCreated(this, e);
                        }

                        // enqueue
                        _resolverqueue.Enqueue(resolver);
                    }

                    Trace("added {0} source(s) to the compilation.", newsources.Count);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool AssessCompilation()
        {
            int errors = 0;
            int warnings = 0;
            foreach (var diag in _compilation.GetDiagnostics())
            {
                if (diag.Severity == DiagnosticSeverity.Error)
                {
                    ReportError(diag);
                    errors++;
                }
                else if (diag.Severity == DiagnosticSeverity.Warning)
                {
                    // Trace("{0}", diag);
                    warnings++;
                }
            }

            Trace("{0} error(s), {1} warning(s).", errors, warnings);

            return 0 == errors;
        }

        #endregion

        #region Conversions

        private string ConvertName(MethodDeclarationSyntax method)
        {
            var name = method.Identifier.Text;
            if (null != method.TypeParameterList)
            {
                name += "`" + method.TypeParameterList.Parameters.Count;
            }

            return name;
        }


        #endregion

        #region Name Extraction

        private bool IsNestedDeclaration(SyntaxNode node)
        {
            return node.Ancestors().OfType<TypeDeclarationSyntax>().Any();
        }

        /// <summary>
        /// Extracts names from a syntax tree.
        /// </summary>
        /// <param name="tree">The syntax tree to recurse.</param>
        private void ExtractNames(SourceInfo source)
        {
            ExtractNames(source.Tree.GetRoot());
        }

        /// <summary>
        /// Extracts names from a collection of nodes.
        /// </summary>
        /// <param name="nodes">Collection of syntax nodes.</param>
        private void ExtractNames(IEnumerable<SyntaxNode> nodes)
        {
            foreach (var node in nodes)
            {
                ExtractNames(node);
            }
        }

        internal void ReportUsingDirective(UsingDirectiveSyntax us)
        {
            ProcessUsingDirective(us, SuctionEventCode.IncludeUsing);
        }

        private void ProcessUsingDirective(UsingDirectiveSyntax ud, SuctionEventCode code)
        {
            var name = string.Intern(ud.Name.ToString());

            var collection = code == SuctionEventCode.ExtractUsing ?
                _extractedusings : _includedusings;

            if (!collection.Contains(name))
            {
                collection.Add(name);

                if (null != OnUsing)
                {
                    var e = new SuctionEventArgs
                    {
                        Code = code,
                        Name = name
                    };

                    OnUsing(this, e);
                }
            }
        }

        /// <summary>
        /// Extracts names from a single syntax node.
        /// </summary>
        /// <param name="node">The node to process.</param>
        private void ExtractNames(SyntaxNode node)
        {
            var descend = true;

            if (node is UsingDirectiveSyntax)
            {
                ProcessUsingDirective((UsingDirectiveSyntax)node, SuctionEventCode.ExtractUsing);
            }
            else if (node is MethodDeclarationSyntax)
            {
                var method = (MethodDeclarationSyntax)node;
                if (method.ParameterList.Parameters.Any(p => p.Modifiers.Any(SyntaxKind.ThisKeyword)))
                {
                    AddName(ConvertName(method), NameRole.ExtensionMethod, method);
                }

                descend = false;
            }
            else if (node is TypeDeclarationSyntax)
            {
                if (!IsNestedDeclaration(node))
                {
                    var typedecl = (TypeDeclarationSyntax)node;
                    var name = typedecl.Identifier.Text;

                    if (null != typedecl.TypeParameterList)
                    {
                        name += "`" + typedecl.TypeParameterList.Parameters.Count;
                    }

                    AddName(name, NameRole.Declaration, typedecl);

                    var isabstract = typedecl.Modifiers.Any(SyntaxKind.AbstractKeyword);

                    if (!isabstract && typedecl.Keyword.Text == "class")
                    {
                        // non-abstract class declaration, register direct interfaces
                        foreach (var bc in node.ChildNodes().OfType<BaseListSyntax>())
                        {
                            foreach (var basetype in bc.Types)
                            {
                                var ifname = basetype.ToString();
                                AddInterfaceImplementation(ifname, typedecl.GetDeclaration());
                            }
                        }
                    }
                    else
                    {
                        // descend = false;
                    }
                }
            }
            else if (node is EnumDeclarationSyntax)
            {
                if (!IsNestedDeclaration(node))
                {
                    var typedecl = (EnumDeclarationSyntax)node;
                    // Trace("type {0} declaration {1}", typedecl.Keyword, typedecl.Identifier);

                    AddName(typedecl.Identifier.Text, NameRole.Declaration, typedecl);
                    descend = false;
                }
            }
            else if (node is DelegateDeclarationSyntax)
            {
                if (!IsNestedDeclaration(node))
                {
                    var decl = (DelegateDeclarationSyntax)node;
                    var name = decl.Identifier.Text;
                    if (null != decl.TypeParameterList)
                    {
                        name += "`" + decl.TypeParameterList.Parameters.Count;
                    }

                    AddName(name, NameRole.Declaration, decl);
                    descend = false;
                }
            }

            if (descend)
            {
                ExtractNames(node.ChildNodes());
            }
        }

        private void AddInterfaceImplementation(string ifname, string implname)
        {
            List<string> list;
            if (!_interfaces.TryGetValue(ifname, out list))
            {
                _interfaces[ifname] = list = new List<string>();
            }

            list.Add(implname);
        }

        private void AddName(string name, NameRole role, SyntaxNode node)
        {
            TraceName("add name '{0}' => {1} [{2}]", name, node.GetDeclaration(), node.GetType().Name);
            _names.Add(name, role, _trees[node.SyntaxTree], node);
        }

        #endregion

        #region Name Utilities

        private string QuoteString(string id)
        {
            return "\"" + id + "\"";
        }

        #endregion

        private bool ValidateSelection()
        {
            foreach (var ambigousinterface in Selection.AmbigousEntries)
            {
                var e = new SuctionEventArgs();
                e.Code = SuctionEventCode.AmbigousFactory;
                e.Name = ambigousinterface;
                e.Mapping = Selection.GetMapping(ambigousinterface);

                if (null != OnDisambiguateMapping)
                {
                    OnDisambiguateMapping(this, e);
                }

                if (e.Mapping.Count() != 1)
                {
                    throw new Exception("interface [" + ambigousinterface + "] needs disambiguation, could be one of: "
                        + e.Mapping.Select(x => x.ImplementationTypeName).ToSeparatorList());
                }

                Selection.ChooseMapping(e.Mapping.First());
            }

            return !Selection.AmbigousEntries.Any();
        }

        private bool ApplySelection()
        {
            ValidateSelection();

            var result = false;
            foreach (var implname in Selection.UnappliedImplementations)
            {
                foreach (var info in LookupName(implname))
                {
                    info.Source.Schedule();
                    result = true;

                    Selection.SetApplied(implname);
                }
            }

            return result;
        }


        /// <summary>
        /// Looks for interface declarations that have the [ObjectFactory] attribute.
        /// </summary>
        private bool ProcessObjectFactoryAttributes()
        {
            var result = false;

            foreach (var tree in _compilation.SyntaxTrees)
            {
                // enumerate all interfaces ...
                var model = _compilation.GetSemanticModel(tree, false);

                foreach (var decl in tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>())
                {
                    var declname = decl.GetDeclaration();
                    if (_checkedinterfacenames.Contains(declname))
                    {
                        continue;
                    }

                    foreach (var attribute in decl.AttributeLists.SelectMany(al => al.Attributes))
                    {
                        var symbol = model.GetDeclaredSymbol(decl);
                        var fullname = string.Intern(symbol.ToString());

                        var info = model.GetTypeInfo(attribute);
                        var isfactory = info.Type.ToDisplayString().Equals("IGRA3.Common.ObjectFactoryAttribute");

                        if (isfactory)
                        {
                            List<string> classlist;
                            if (_interfaces.TryGetValue(symbol.Name, out classlist))
                            {
                                foreach (var classname in classlist)
                                {
                                    _selection.Add(fullname, classname);
                                }
                            }
                        }
                    }

                    _checkedinterfacenames.Add(declname);
                }
            }

            return result;
        }

        private void ProcessInitializationAttibutes()
        {
            foreach (var tree in _compilation.SyntaxTrees)
            {
                var model = _compilation.GetSemanticModel(tree, false);
                foreach (var decl in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    foreach (var attribute in decl.AttributeLists.SelectMany(al => al.Attributes))
                    {
                        var symbol = model.GetDeclaredSymbol(decl);
                        var fullname = string.Intern(symbol.ToString());

                        var info = model.GetTypeInfo(attribute);

                        if (info.Type.ToDisplayString().Equals("IGRA3.Common.InitializeOnBootAttribute"))
                        {
                            _initonboot.Add(decl);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates initializer code for the object factory.
        /// </summary>
        /// <returns></returns>
        public void CreateInitializer()
        {
            var of = Selection.GetChosenMappings();

            var entrypoint = _compilation.GetEntryPoint(new System.Threading.CancellationToken());

            if (null == entrypoint)
            {
                ReportError("no entrypoint specified, initializer not added.");
                return;
            }

            var initcode = new StringBuilder();
            initcode.AppendLine("using IGRA3.Common;");
            initcode.AppendLine("using System.Reflection;");

            // class ObjectFactoryStartup
            initcode.AppendLine("class ObjectFactoryStartup {");

            // replacement 'Main'
            initcode.AppendLine("static void Main(string[] args) {");
            initcode.AppendLine("Initialize();");
            if (entrypoint.Parameters.Any())
            {
                initcode.AppendLine(entrypoint.ContainingSymbol + "." + entrypoint.Name + "(args);");
            }
            else
            {
                initcode.AppendLine(entrypoint.Name + "();");
            }

            initcode.AppendLine("}");

            // method 'Initialize'
            initcode.AppendLine("internal static void Initialize() {");

            initcode.AppendLine("ObjectFactory.DisableInitialization = true;");
            initcode.AppendLine("ObjectFactory.AddSearchAssembly(Assembly.GetExecutingAssembly());");

            foreach (var o in of)
            {
                initcode.AppendLine("ObjectFactory.Register("
                    + QuoteString(o.InterfaceTypeName) + ", "
                    + QuoteString(o.ImplementationTypeName) + ");");
            }

            foreach (var i in _initonboot)
            {
                var declname = i.GetDeclaration();
                initcode.AppendLine("System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(" + declname + ").TypeHandle);");
            }

            initcode.AppendLine("TraceDispatcher.Initialize();");
            initcode.AppendLine("TraceDispatcher.DisableConsoleOutput = true;");

            initcode.AppendLine("}");

            // end ObjectFactoryStartup
            initcode.AppendLine("}");

            // class ObjectFactoryInitialization, used by test execution
            initcode.AppendLine("public class ObjectFactoryInitialization : System.MarshalByRefObject {");
            initcode.AppendLine("public void Configure() { ObjectFactoryStartup.Initialize(); }");
            initcode.AppendLine("}");

            var newentrypoint = "ObjectFactoryStartup";

            var workingdirectory = Directory.GetCurrentDirectory();
            var filename = "factory.g.cs";
            var initfile = Path.Combine(workingdirectory, filename);
            File.WriteAllText(initfile, initcode.ToString());

            AddSourceFile(filename).Schedule();

            var options = _compilation.Options.WithMainTypeName(newentrypoint);

            // extend the compilation tree
            _compilation = _compilation
                .WithOptions(options);
        }

        #region Error Handling

        private void ReportError(Diagnostic diag)
        {
            if (diag.WarningLevel == 0)
            {
                Trace("{0}", diag);
            }

            if (null != OnCompilationError)
            {
                var e = new SuctionEventArgs();
                e.Code = SuctionEventCode.CompilationError;
                e.Message = diag.GetMessage();
                OnCompilationError(this, e);
            }
        }

        private void ReportError(string message)
        {
            Trace("{0}", message);
            if (null != OnCompilationError)
            {
                var e = new SuctionEventArgs();
                e.Code = SuctionEventCode.CompilationError;
                e.Message = message;
                OnCompilationError(this, e);
            }
        }

        #endregion

        #endregion
    }
}
