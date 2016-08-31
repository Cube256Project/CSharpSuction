using Common;
using CSharpSuction.Configuration;
using CSharpSuction.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace CSharpSuction
{
    /// <summary>
    /// The settings for a suction project.
    /// </summary>
    public class ProjectDescriptor
    {
        #region Private

        private List<string> _goals = new List<string>();
        private List<SourceInclude> _includes = new List<SourceInclude>();
        private List<EmitInstruction> _emits = new List<EmitInstruction>();
        private List<string> _referencepath = new List<string>();
        private List<string> _references = new List<string>();
        private Dictionary<string, string> _factorysettings = new Dictionary<string, string>();
        private Dictionary<string, string> _defines = new Dictionary<string, string>();
        private HashSet<string> _resourcextensions = new HashSet<string>();

        private string _firstoriginal = null;
        private Stack<string> _originaldirectory = new Stack<string>();
        private Stack<string> _sourcedirectory = new Stack<string>();

        #endregion

        #region Properties

        public string OriginalDirectory
        {
            get { return _originaldirectory.Any() ? _originaldirectory.Peek() : _firstoriginal; }
        }


        /// <summary>
        /// Equals to OriginalDirectory for outside this class.
        /// </summary>
        public string SourceDirectory
        {
            get
            {
                return _sourcedirectory.Any() ? _sourcedirectory.Peek() : OriginalDirectory;
            }
        }

        /// <summary>
        /// Names of requested classes.
        /// </summary>
        public IList<string> Goals { get { return _goals; } }

        /// <summary>
        /// Include directories, absolute path.
        /// </summary>
        public IList<SourceInclude> Includes { get { return _includes; } }

        public IList<EmitInstruction> Emits {  get { return _emits; } }

        public IList<string> References { get { return _references; } }

        public IList<string> ReferencePath { get { return _referencepath; } }

        public IEnumerable<KeyValuePair<string, string>> Defines { get { return _defines; } }

        public ICollection<string> ResourceExtensions {  get { return _resourcextensions; } }

        public string Version { get; set; }

        public string EntryPoint { get; set; }

        public bool EnableObjectFactory { get; set; }

        public string OutputName { get; set; }

        /// <summary>
        /// Root of the output.
        /// </summary>
        public string OutputBaseDirectory { get; set; }

        /// <summary>
        /// Output path relative to root.
        /// </summary>
        public string OutputDirectory { get; set; }

        public string OutputKind { get; set; }

        public bool IsLoaded { get; set; }

        protected XmlReader Reader { get; private set; }

        #endregion

        #region Public Methods

        /*public void DisambiguateMapping(object sender, SuctionEventArgs e)
        {
            string implname;
            if (_factorysettings.TryGetValue(e.Name, out implname))
            {
                e.Mapping = e.Mapping.Where(g => g.ImplementationTypeName == implname);
            }
        }*/

        public void AddSourceDirectory(string path)
        {
            path = path.MakeAbsolutePath();

            if (null == _firstoriginal)
            {
                _firstoriginal = path;
            }

            var include = new SourceInclude();
            include.AbsolutePath = path;

            _includes.Add(include);
        }

        #region Serialization

        public void Load(string filename)
        {
            string container;
            if (Path.IsPathRooted(filename))
            {
                container = Path.GetDirectoryName(filename);
            }
            else if(_originaldirectory.Any())
            {
                container = _originaldirectory.Peek();
                filename = Path.Combine(container, filename);
            }
            else
            {
                throw new ArgumentException("absolute path required.");
            }

            var previousreader = Reader;
            try
            {
                // top level context stack
                _originaldirectory.Push(container);

                if(null == _firstoriginal)
                {
                    _firstoriginal = container;
                }

                Log.Debug("processing project file {0} ...", filename.Quote());
                
                using (Reader = XmlReader.Create(filename))
                {
                    Load();
                }
            }
            finally
            {
                _originaldirectory.Pop();

                Reader = previousreader;
            }
        }

        public void Load()
        {
            Reader.MoveToContent();

            while (Reader.NodeType != XmlNodeType.None)
            {
                if (Reader.NodeType == XmlNodeType.Element)
                {
                    if (Reader.LocalName == "Source")
                    {
                        ReadSourceElement();
                    }
                    else if (Reader.LocalName == "SourceDirectory")
                    {
                        ReadSourceDirectoryElement();
                    }
                    else if (Reader.LocalName == "ResourceExtensions")
                    {
                        ReadResourceExtensionsElement();
                    }
                    else if (Reader.LocalName == "Goal")
                    {
                        ReadGoalElement();
                    }
                    else if (Reader.LocalName == "Emit")
                    {
                        ReadEmitElement();
                    }
                    else if (Reader.LocalName == "EntryPoint")
                    {
                        string name;
                        if (Reader.IsStartElement() && !Reader.IsEmptyElement)
                        {
                            name = Reader.ReadElementContentAsString();
                        }
                        else
                        {
                            name = Reader.GetAttribute("Name");
                            Reader.Skip();
                        }

                        _goals.Add(name);
                        EntryPoint = name;
                    }
                    else if (Reader.LocalName == "Assembly")
                    {
                        _references.Add(Reader.GetAttribute("Name").Trim());
                        Reader.Skip();
                    }
                    else if (Reader.LocalName == "ReferencePath")
                    {
                        _referencepath.Add(Reader.GetAttribute("Path").Trim());
                        Reader.Skip();
                    }
                    else if (Reader.LocalName == "Factory")
                    {
                        var ifname = Reader.GetAttribute("Interface");
                        var implname = Reader.GetAttribute("Implementation");

                        _factorysettings[ifname] = implname;
                        Reader.Skip();
                    }
                    else if (Reader.LocalName == "ObjectFactory")
                    {
                        EnableObjectFactory = true;
                        Reader.Skip();
                    }
                    else if (Reader.LocalName == "Output")
                    {
                        ReadOutputElement();
                    }
                    else if (Reader.LocalName == "Define")
                    {
                        ReadDefineElement();
                    }
                    else if (Reader.LocalName == "Include")
                    {
                        ReadIncludeElement();
                    }
                    else if (Reader.IsStartElement() && !Reader.IsEmptyElement)
                    {
                        Reader.ReadStartElement();
                    }
                    else
                    {
                        Reader.Skip();
                    }
                }
                else if (Reader.NodeType == XmlNodeType.EndElement)
                {
                    if (Reader.LocalName == "SourceDirectory" && _sourcedirectory.Any())
                    {
                        _sourcedirectory.Pop();
                    }

                    Reader.ReadEndElement();
                }
                else
                {
                    Reader.Read();
                }
            }

            IsLoaded = true;
        }

        private void ReadEmitElement()
        {
            var emit = new EmitInstruction();

            if (Reader.MoveToFirstAttribute())
            {
                do
                {
                    switch (Reader.LocalName)
                    {
                        case "Type":
                            emit.Kind = Reader.Value;
                            break;

                        case "Destination":
                            emit.Destination = Reader.Value;
                            break;

                        default:
                            emit.Parameters.Add(Reader.LocalName, Reader.Value);
                            break;
                    }
                }
                while (Reader.MoveToNextAttribute());
                Reader.MoveToContent();
            }

            _emits.Add(emit);

            Reader.Skip();
        }

        private void ReadIncludeElement()
        {
            foreach (var filename in SplitNameList(Reader.ReadElementContentAsString()))
            {
                Load(filename);
            }
        }

        private void ReadOutputElement()
        {
            OutputDirectory = Reader.GetAttribute("Directory") ?? OutputDirectory;
            OutputName = Reader.GetAttribute("Name") ?? OutputName;
            OutputKind = Reader.GetAttribute("Kind") ?? OutputKind;
            Version = Reader.GetAttribute("Version") ?? Version;
            Reader.Skip();
        }

        private void ReadDefineElement()
        {
            var defname = Reader.GetAttribute("Name");
            string defvalue = null;
            if (Reader.IsStartElement() && !Reader.IsEmptyElement)
            {
                defvalue = Reader.ReadElementContentAsString().Trim();
            }
            else
            {
                Reader.Skip();
            }

            _defines[defname] = defvalue;
        }

        private void ReadGoalElement()
        {
            _goals.AddRange(SplitNameList(Reader.GetAttribute("Names")));
            if (Reader.IsStartElement() && !Reader.IsEmptyElement)
            {
                _goals.AddRange(SplitNameList(Reader.ReadElementContentAsString()));
            }
            else
            {
                Reader.Skip();
            }
        }

        private void ReadResourceExtensionsElement()
        {
            string includes;
            if (Reader.IsStartElement() && !Reader.IsEmptyElement)
            {
                includes = Reader.ReadElementContentAsString();
            }
            else
            {
                throw new SuctionConfigurationException("missing content of 'ResourceExtensions' element.");
            }

            foreach (var name in SplitNameList(includes))
            {
                if (name.Contains('*'))
                {
                    throw new SuctionConfigurationException("wildcards not allowed, use '.ext'.");
                }

                _resourcextensions.Add(name);
            }
        }

        private void ReadSourceDirectoryElement()
        {
            if (Reader.IsStartElement() && !Reader.IsEmptyElement)
            {
                var rn = Reader.GetAttribute("Directory");

                // replace macros in directory path
                rn = null == rn ? null : ReplaceMacros(rn);

                string path = SourceDirectory;

                if (null != rn)
                {
                    if (!Path.IsPathRooted(rn))
                    {
                        // relative to source
                        path = Path.Combine(path, rn);
                    }
                    else
                    {
                        Log.Warning("absolute path {0} found.", rn);
                        path = rn;
                    }
                }

                // enter source directory
                _sourcedirectory.Push(path);

                Reader.ReadStartElement();
            }
            else
            {
                Reader.Skip();
            }
        }

        private void ReadSourceElement()
        {
            string includes;
            string filter;
            if (Reader.IsStartElement() && !Reader.IsEmptyElement)
            {
                filter = Reader.GetAttribute("Filter");
                includes = Reader.ReadElementContentAsString();
            }
            else
            {
                throw new SuctionConfigurationException("missing content of 'Source' element.");
            }

            // separated list of path strings ...
            foreach (var path in SplitNameList(includes))
            {
                var top = _sourcedirectory.Count > 0 ? _sourcedirectory.Peek() : string.Empty;
                var include = new SourceInclude();
                include.AbsolutePath = Path.IsPathRooted(path) ? path : Path.Combine(top, path);
                include.Filter = string.IsNullOrEmpty(filter) ? null : filter;

                // Log.Debug("add include {0} filter {1}.", include.Name, include.Filter);

                _includes.Add(include);
            }
        }

        #endregion

        #endregion

        #region Private Methods

        private IEnumerable<string> SplitNameList(string arg)
        {
            if (!string.IsNullOrEmpty(arg))
            {
                return arg
                    .Split(new char[] { ';', ',', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim());
            }
            else
            {
                return new string[0];
            }
        }

        private static Regex _macrorx = new Regex(@"\$\(?<tag>[\w\d_]+\)");

        private string ReplaceMacros(string s)
        {
            return _macrorx.Replace(s, m => ResolveMacro(m.Groups["tag"].Value));
        }

        private string ResolveMacro(string value)
        {
            switch(value)
            {
                case "OriginalDirectory":
                    return OriginalDirectory;

                default:
                    throw new ArgumentException("suction project macro " + value.Quote() + " is undefined.");
            }
        }

        #endregion
    }
}
