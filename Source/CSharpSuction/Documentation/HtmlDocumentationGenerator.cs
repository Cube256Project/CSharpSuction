using Common;
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

namespace CSharpSuction.Documentation
{
    /// <summary>
    /// Step 2: Generates HTML from intermediate documentation format.
    /// </summary>
    class HtmlDocumentationGenerator
    {
        #region Private

        private Dictionary<string, Topic> _topics = new Dictionary<string, Topic>();
        private XmlDocument _index;
        private static XslCompiledTransform _transform;
        private Dictionary<string, List<Topic>> _namemap = new Dictionary<string, List<Topic>>();
        private Tree<Topic> _tree = new Tree<Topic>();
        private HashSet<string> _unresolved = new HashSet<string>();


        #endregion

        #region Properties

        public bool WriteLogFile = true;

        public ResultWriter Results { get; set; }

        #endregion

        #region Construction

        static HtmlDocumentationGenerator()
        {
            _transform = new XslCompiledTransform();
            using (var reader = XmlReader.Create(typeof(HtmlDocumentationGenerator)
                .Assembly.GetResourceStream("IntermediateToHtml.xslt")))
            {
                _transform.Load(reader);
            }
        }

        public HtmlDocumentationGenerator()
        {
            Results = new ResultWriter();
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

        #endregion

        public void Generate()
        {
            if (null == Intermediate)
            {
                throw new ArgumentNullException("intermediate document was not set.");
            }

            if (WriteLogFile)
            {
                var logfile = Path.Combine(OutputDirectory, "dump.xml");
                using (var writer = XmlWriter.Create(logfile, new XmlWriterSettings { Indent = true }))
                {
                    Intermediate.WriteTo(writer);
                }
            }

            // prepare index document
            _index = new XmlDocument();
            var indexroot = _index.CreateElement("Index", EmitDocumentation.IntermediateNamespaceURI);
            _index.AppendChild(indexroot);

            AddTopicElement("$", _index.DocumentElement);

            // enumerate topics ...
            foreach (var e in Intermediate.DocumentElement.ChildNodes.OfType<XmlElement>())
            {
                if (e.LocalName == "TypeDocumentation" || e.LocalName == "ExternalDocumentation")
                {
                    var fullname = e.GetAttribute("Key");
                    if (!string.IsNullOrEmpty(fullname))
                    {
                        AddTopicElement(fullname, e);
                    }
                }
            }

            // setup index
            foreach (var topic in _topics.OrderBy(e => e.Key).Select(e => e.Value))
            {
                var indexentry = _index.CreateElement("TopicReference", EmitDocumentation.IntermediateNamespaceURI);
                indexentry.SetAttribute("Key", topic.Key);
                indexentry.InnerText = topic.Title;
                indexroot.AppendChild(indexentry);
            }

            // callback object for XSL transform
            var arglist = new XsltArgumentList();
            arglist.AddExtensionObject("cfs:documentation-callback", new HtmlDocumentationCallback(this));

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
                if (name == "$") name = "Index";

                var outputfile = Path.Combine(OutputDirectory, name + ".html");

                // generate XML dump ...
                if(WriteLogFile)
                {
                    WriteTopicLogFile(tobet, name);
                }

                Trace("{0} -> {1} ...", topic.Key, outputfile);

                var dumpy = new StringBuilder();

                using (var reader = XmlReader.Create(new StringReader(tobet.ToString())))
                using (var writer = XmlWriter.Create(outputfile, GetSettings()))
                {
                    _transform.Transform(reader, arglist,  writer);
                }
            }

            Log.Information("documentation created {0} topic(s), output to {1}.", 
                _topics.Count, OutputDirectory.Quote());
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

        private static Regex _extensionfilter = new Regex(@".html$");

        internal string TranslateReference(string cref)
        {
            string result = null;

            cref = _extensionfilter.Replace(cref, m => string.Empty);

            // extract last part of name ...
            var name = cref.Split('.').Last();

            List<Topic> nlist;
            if (_namemap.TryGetValue(name, out nlist))
            {
                var cand = nlist.Where(e => e.Key.EndsWith(cref));
                var count = cand.Count();
                if (count > 0)
                {
                    if(count > 1)
                    {
                        // Log.Warning("[TranslateReference] ambigous cref {0}", cref.Quote());
                    }

                    var sel = cand.First();
                    result =  sel.Key + ".html";
                }
            }

            if (null == result)
            {
                if (_unresolved.Add(cref))
                {
                    // only once per cref

                    var log = Log.Create().WithHeader("failed to resolve reference {0}.", cref.Quote());
                    if (cref.StartsWith("System."))
                    {
                        // debug only
                    }
                    else if (cref.StartsWith("Newtonsoft.Json."))
                    {
                        // debug only
                    }
                    else
                    {
                        log = log.WithSeverity(LogSeverity.warning);
                    }

                    Results.Write(new UnresolvedDocumentationReferenceResult(cref, log));
                }
            }

            result = result ?? "#unresolved";

            // Trace("[TranslateReference] {0} -> {1}", cref, result);

            return result;
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

                var parts = key.Split('.');
                var name = parts.Last();

                RegisterTopicByName(topic, name);
            }

            topic.Nodes.Add(e);

            if (key != "$")
            {
                // create index entry unless index
                var entryindex = _index.CreateElement("Topic");
                entryindex.SetAttribute("Location", key);
            }
        }

        private void RegisterTopicByName(Topic topic, string name)
        {
            List<Topic> nlist;
            if (!_namemap.TryGetValue(name, out nlist))
            {
                _namemap.Add(name, nlist = new List<Topic>());
            }

            nlist.Add(topic);
        }

        private string DeriveDocIDFromClassName(string key)
        {
            // Q3F7OZUOAP: compose doc-id
            return "cfs:help:current-version:" + key;
        }

        #endregion
    }
}
