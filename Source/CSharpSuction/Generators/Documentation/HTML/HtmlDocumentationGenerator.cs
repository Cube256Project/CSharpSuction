using Common;
using CSharpSuction.Generators.Documentation.Topics;
using CSharpSuction.Results;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Xsl;

namespace CSharpSuction.Generators.Documentation.HTML
{
    /// <summary>
    /// Step 2: Generates HTML from intermediate documentation format.
    /// </summary>
    class HtmlDocumentationGenerator
    {
        #region Private

        private Dictionary<string, Topic> _topics = new Dictionary<string, Topic>();
        private static XslCompiledTransform _transform;
        private TopicTable _namemap = new TopicTable();
        private Tree<Topic> _tree = new Tree<Topic>();
        private HashSet<string> _unresolved = new HashSet<string>();
        private HashSet<string> _noncritical = new HashSet<string>();


        #endregion

        #region Properties

        public bool WriteLogFile = true;

        public ResultWriter Results { get; set; }

        public ICollection<string> NonCriticalNamespaces { get { return _noncritical; } }

        public string EntryPage { get; set; }

        #endregion

        #region Construction

        static HtmlDocumentationGenerator()
        {
            // load the main XSLT
            var t = new XslCompiledTransform();
            using (var reader = XmlReader.Create(typeof(HtmlDocumentationGenerator)
                .Assembly.GetResourceStream("IntermediateToHtml.xslt")))
            {
                t.Load(reader);
            }

            // keep for this domain
            _transform = t;
        }

        public HtmlDocumentationGenerator()
        {
            // allocate a results writer, may be replaced
            Results = new ResultWriter();

            // non-critical namespace
            _noncritical.Add("System");
            _noncritical.Add("Newtonsoft.JSON");
            _noncritical.Add("Microsoft");
        }

        #endregion

        #region Diagnostics

        [Conditional("VERBOSE")]
        private void Trace(string format, params object[] args)
        {
            Log.Trace(format, args);
        }

        #endregion

        #region Properties

        public string OutputDirectory { get; set; }

        public XmlDocument Intermediate { get; set; }

        public IReadOnlyDictionary<string, string> Parameters { get; private set; }

        #endregion

        public void SetParameters(IReadOnlyDictionary<string, string> parameters)
        {
            string entrypage;
            if (parameters.TryGetValue("EntryPage", out entrypage))
            {
                EntryPage = entrypage;
            }

            Parameters = parameters;
        }

        public void Generate()
        {
            if (null == Intermediate)
            {
                throw new ArgumentNullException("intermediate document was not set.");
            }

            GenerateTableOfContents();

            if (WriteLogFile)
            {
                var logfile = Path.Combine(OutputDirectory, "dump.xml");
                using (var writer = XmlWriter.Create(logfile, new XmlWriterSettings { Indent = true }))
                {
                    Intermediate.WriteTo(writer);
                }
            }

            // callback object for XSL transform
            var arglist = new XsltArgumentList();
            arglist.AddExtensionObject("cfs:documentation-callback", new HtmlDocumentationCallback(this));

            var indexfound = false;

            // transform topics individually
            foreach (var topic in _topics.Values)
            {
                var tobet = new StringBuilder();
                using (var writer = XmlWriter.Create(tobet, GetSettings()))
                {
                    writer.WriteStartElement("Topic", EmitDocumentation.IntermediateNamespaceURI);

                    if (null != topic.DocID)
                    {
                        writer.WriteAttributeString("DocID", topic.DocID);
                    }

                    foreach (var e in topic.Nodes)
                    {
                        e.WriteTo(writer);
                    }
                }

                // Log.Debug("tobet:\n{0}", tobet);

                var name = topic.Key;

                if (name == EntryPage)
                {
                    name = "Index";
                    indexfound = true;
                }

                var outputfile = Path.Combine(OutputDirectory, name + ".html");

                // generate XML dump ...
                if (WriteLogFile)
                {
                    WriteTopicLogFile(tobet, name);
                }

                var dumpy = new StringBuilder();

                using (var reader = XmlReader.Create(new StringReader(tobet.ToString())))
                using (var writer = XmlWriter.Create(outputfile, GetSettings()))
                {
                    _transform.Transform(reader, arglist, writer);
                }
            }

            if (!indexfound)
            {
                Log.Warning("no EntryPage specified.");
            }

            Log.Information("documentation created {0} topic(s), output to {1}.",
                _topics.Count, OutputDirectory.Quote());
        }

        private void GenerateTableOfContents()
        {
            // prepare index document
            var dom = Intermediate;
            var toc = dom.CreateElement("TableOfContents", EmitDocumentation.IntermediateNamespaceURI);
            dom.DocumentElement.AppendChild(toc);

            AddTopicElement("TableOfContents", toc);

            // enumerate topics ...
            foreach (var e in Intermediate.DocumentElement.ChildNodes.OfType<XmlElement>())
            {
                if (e.LocalName == "TypeDocumentation" || e.LocalName == "ExternalDocumentation")
                {
                    var fullname = e.GetAttribute("Key");
                    if (!string.IsNullOrEmpty(fullname))
                    {
                        // add to topic list
                        AddTopicElement(fullname, e);
                    }
                }
            }

            // setup index
            foreach (var topic in _topics.OrderBy(e => e.Key).Select(e => e.Value))
            {
                var indexentry = dom.CreateElement("TopicReference", EmitDocumentation.IntermediateNamespaceURI);
                var link = dom.CreateElement("TopicLink", EmitDocumentation.IntermediateNamespaceURI);
                link.SetAttribute("RefKey", topic.Key);
                link.SetAttribute("Name", topic.Title);
                indexentry.AppendChild(link);
                toc.AppendChild(indexentry);
            }
        }

        private void WriteTopicLogFile(StringBuilder tobet, string name)
        {
            // translate to get indented output (more readable)
            var doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(tobet.ToString());
            var xmlfile = Path.Combine(OutputDirectory, name + ".xml");
            using (var writer = XmlWriter.Create(xmlfile, GetSettings(true)))
            {
                doc.WriteTo(writer);
            }
        }

        #region Internal Methods


        internal string TranslateReference(string cref)
        {
            // resulting value; a relative URI
            string result = null;

            try
            {
                // parse the reference string
                var tref = TopicReference.Parse(cref);

                // get associated topics
                var topics = _namemap.GetTopics(tref);
                if (!topics.Any())
                {
                    // not found, report once
                    if (_unresolved.Add(cref))
                    {
                        // make a warning
                        var log = Log.Create().WithHeader("failed to resolve reference {0}.", cref.Quote());

                        // discriminate non-critical namespaces
                        if (!_noncritical.Any(n => tref.Namespaces.Any(q => n == q)))
                        {
                            log = log.WithSeverity(LogSeverity.warning);
                        }

                        Results.Write(new UnresolvedDocumentationReferenceResult(cref, log));
                    }
                }
                else if(topics.Count() > 1)
                {
                    // Log.Warning("found multiple topics for {0}, using first of {1}.", cref.Quote(), topics.Select(t => t.Key).ToSeparatorList());
                }

                var topic = topics.FirstOrDefault();
                if (null != topic)
                {
                    result = topic.TranslateReference(tref) + ".html";
                }
            }
            catch (Exception ex)
            {
                Results.Write(new TopicReferenceTranslationError(cref, ex));
            }

            // default result
            return result ?? "#unresolved";
        }

        #endregion

        #region Private Methods

        private XmlWriterSettings GetSettings(bool indent = false)
        {
            return new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = indent
            };
        }

        /// <summary>
        /// Associates a XML element with a topic.
        /// </summary>
        /// <param name="key">The topic key.</param>
        /// <param name="e">The element to add.</param>
        private void AddTopicElement(string key, XmlElement e)
        {
            // fix topic ...
            Topic topic;
            if (!_topics.TryGetValue(key, out topic))
            {
                // create a new topic
                topic = new Topic(key);
                topic.DocID = DeriveDocIDFromClassName(key);

                // main table
                _topics.Add(key, topic);

                // add to name table
                _namemap.AddTopic(topic);
            }

            topic.Nodes.Add(e);

            /*if (key != "$")
            {
                // create index entry unless index
                var entryindex = _index.CreateElement("Topic");
                entryindex.SetAttribute("Location", key);
            }*/ 
        }

        private string DeriveDocIDFromClassName(string key)
        {
            // Q3F7OZUOAP: compose doc-id
            return "cfs:help:current-version:" + key;
        }

        #endregion
    }
}
