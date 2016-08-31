using Common;
using CSharpSuction.Generators.Documentation.Topics;
using CSharpSuction.Input;
using CSharpSuction.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace CSharpSuction.Generators.Documentation
{
    /// <summary>
    /// Step 1: Converts types of a suction into intermediate documentation format.
    /// </summary>
    /// <remarks>
    /// <para>The namespace URI for the intermediate format is 'cfs:documentation-1591'.</para>
    /// <para>The result is subsequently processed by the <see cref="HtmlDocumentationGenerator"/>.</para>
    /// </remarks>
    public class HtmlDocumentationBuilder
    {
        #region Private

        private TopicLinkCollection _relations = new TopicLinkCollection();
        private HashSet<string> _namespaces = new HashSet<string>();

        private enum TriviaState { initial, start, line };

        TriviaState _tstate;
        int _tindent;
        int _teat;

        #endregion

        #region Properties

        private XmlWriter Writer { get; set; }

        private Suction Suction { get; set; }

        private SemanticModel Model { get; set; }

        private bool WhitespaceNeeded { get; set; }

        private string CurrentTypeName { get; set; }

        #endregion

        #region Diagnostics

        [Conditional("VERBOSE")]
        private void Trace(string format, params object[] args)
        {
            Log.Debug(format, args);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Performs the conversion process.
        /// </summary>
        /// <param name="suction">The suction where to take types from.</param>
        /// <returns>The XML document containing the intermediate content.</returns>
        public XmlDocument Build(Suction suction)
        {
            // build documentation topic tree 'cfs:documentation-1591' ...
            var xml = new StringBuilder();
            try
            {
                // from the supplied suction
                Suction = suction;

                // use an XMLWriter to build the content ...
                Writer = XmlWriter.Create(xml, GetSettings());

                // top level element
                Writer.WriteStartElement("DocumentationTopicTree", EmitDocumentation.IntermediateNamespaceURI);

                // add all types in the suction
                foreach (var type in suction.Types.OfType<TypeInfo>())
                {
                    Trace("generating type documentation {0} ...", type.QualifiedName.Quote());
                    EmitTypeDocumentation(type);
                }

                // embedded resource topics, process if applicable
                foreach (var source in suction.Sources.OfType<EmbeddedResourceSourceInfo>())
                {
                    EmitTopicDocumentation(source);
                }

                // generate a topic for each namespace
                GenerateNamespaceTopics();
            }
            finally
            {
                if (null != Writer)
                {
                    Writer.Dispose();
                    Writer = null;
                }

                Suction = null;
            }

            // load result into DOM
            var dom = new XmlDocument();
            dom.PreserveWhitespace = true;
            dom.LoadXml(xml.ToString());

            // supply topic relations
            SupplyTopicReferences(dom);

            // dom result containing everything
            return dom;
        }

        #endregion

        #region Resource Topics

        private void EmitTopicDocumentation(EmbeddedResourceSourceInfo source)
        {
            var extension = Path.GetExtension(source.FullPath).ToLower();
            switch (extension)
            {
                case ".html":
                case ".xsd":
                case ".xslt":
                    EmitTopicXML(source.FullPath);
                    break;
            }
        }

        private void EmitTopicXML(string fullpath)
        {
            try
            {
                var dom = new XmlDocument();
                dom.PreserveWhitespace = true;
                dom.Load(fullpath);

                var ns = dom.DocumentElement.NamespaceURI;

                switch (ns)
                {
                    case "http://www.w3.org/1999/xhtml":
                        Trace("loading HTML resource {0} ...", fullpath.Quote());
                        EmitTopicDocumentationHTML(dom);
                        break;

                    default:
                        throw new Exception("unabled to process XML namespace " + ns.Quote() + ".");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("failed to load {0}: {1}", fullpath.Quote(), ex.Message);
            }
        }

        private void EmitTopicDocumentationHTML(XmlDocument html)
        {
            var tag = "cfs:relative:docid";

            var nm = new XmlNamespaceManager(html.NameTable);
            nm.AddNamespace("h", "http://www.w3.org/1999/xhtml");

            // look for DocID
            var docid = html
                .SelectNodes("/h:html/h:head/h:meta[@name=" + tag.Quote() + "]/@content", nm)
                .OfType<XmlAttribute>()
                .FirstOrDefault();

            if (null == docid)
            {
                throw new Exception("missing " + tag.Quote() + " META element.");
            }

            var title = html
                .SelectNodes("/h:html/h:head/h:title", nm)
                .OfType<XmlElement>()
                .FirstOrDefault();

            if (null == title)
            {
                throw new Exception("missing TITLE element, required.");
            }

            var key = docid.Value;

            Trace("TODO: add external topic {0} ...", key);

            Writer.WriteStartElement("ExternalDocumentation");
            Writer.WriteAttributeString("Key", key);
            Writer.WriteAttributeString("Name", title.InnerText);

            foreach (var body in html
                .SelectNodes("/h:html/h:body", nm)
                .OfType<XmlElement>())
            {
                Writer.WriteStartElement("Body");
                body.WriteContentTo(Writer);
                Writer.WriteEndElement();
            }

            Writer.WriteEndElement();
        }

        #endregion

        #region Topic References

        private void SupplyTopicReferences(XmlDocument dom)
        {
            // build a map (key -> element)
            var dict = new Dictionary<string, XmlElement>();

            foreach (var e in dom.DocumentElement.ChildNodes.OfType<XmlElement>())
            {
                if (e.LocalName == "TypeDocumentation")
                {
                    dict.Add(e.GetAttribute("Key"), e);
                }
            }

            // populate
            foreach (var r in _relations)
            {
                SupplyTopicLink(dict, r);
            }
        }

        private void SupplyTopicLink(IDictionary<string, XmlElement> dict, TopicLink r)
        {
            XmlElement from, to;

            if (!dict.TryGetValue(r.ReferingTopic, out from))
            {
                return;
            }

            if (!dict.TryGetValue(r.ReferencedTopic, out to))
            {
                return;
            }

            var name = to.GetAttribute("Name");

            var dom = from.OwnerDocument;

            // emit reference
            var topicref = dom.CreateElement("TopicReference", EmitDocumentation.IntermediateNamespaceURI);
            topicref.SetAttribute("Kind", r.Kind.Name);

            if (to.HasAttribute("Kind"))
            {
                topicref.AppendChild(dom.CreateTextNode(to.GetAttribute("Kind")));
                topicref.AppendChild(dom.CreateTextNode(" "));
            }

            // emit link ...
            var link = dom.CreateElement("TopicLink", EmitDocumentation.IntermediateNamespaceURI);
            link.SetAttribute("RefKey", r.ReferencedTopic);
            link.SetAttribute("Name", name);
            topicref.AppendChild(link);

            // ... into left topic element
            from.AppendChild(topicref);
        }

        /// <summary>
        /// Adds a relation from the current topic to another topic.
        /// </summary>
        /// <param name="othertypename"></param>
        /// <param name="kind"></param>
        private void AddTopicRelation(string othertypename, TopicRelation kind)
        {
            _relations.Add(new TopicLink(CurrentTypeName, kind, othertypename));

            if (kind is TopicIsDerivedFrom)
            {
                _relations.Add(new TopicLink(othertypename, new TopicIsBaseClassOf(), CurrentTypeName));
            }
            else if (kind is TopicContainingNamespace)
            {
                _relations.Add(new TopicLink(othertypename, new TopicNamespaceContains(), CurrentTypeName));
            }
        }

        #endregion

        #region Namespaces

        private void GenerateNamespaceTopics()
        {
            foreach (var n in _namespaces)
            {
                Trace("generating namespace {0} ...", n.Quote());

                var parts = n.Split('.');

                Writer.WriteStartElement("TypeDocumentation");
                Writer.WriteAttributeString("Key", n);
                Writer.WriteAttributeString("Name", parts.Last());
                Writer.WriteAttributeString("Kind", "Namespace");
                Writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Enumerates the namespaces containing a type.
        /// </summary>
        /// <param name="decl">The type declaration.</param>
        /// <param name="action">Action to perform on each element.</param>
        private void EmitNamespaceComponents(TypeDeclarationSyntax decl, Action<string, IEnumerable<string>, bool> action)
        {
            var list = new List<string>();
            foreach (var anc in decl.Ancestors().OfType<NamespaceDeclarationSyntax>())
            {
                // prepend
                list.InsertRange(0, anc.Name.ToString().Split('.'));
            }

            for (int j = 0; j < list.Count; ++j)
            {
                action(list[j], list.Take(j + 1), j + 1 == list.Count);
            }
        }

        private void EmitNamespaceComponentLink(string name, IEnumerable<string> elements, bool innermost)
        {
            var fullname = elements.ToSeparatorList(".");

            if (innermost)
            {
                // binding current topic to that namespace
                AddTopicRelation(fullname, new TopicContainingNamespace());
            }

            var count = elements.Count();
            if (count > 1)
            {
                // namespace/namespace containement
                var container = elements.Take(count - 1).ToSeparatorList(".");
                _relations.Add(new TopicLink(container, new TopicNamespaceContains(), fullname));
            }

            // remember this namespace
            _namespaces.Add(fullname);
        }

        #endregion

        #region Emit Intermediate Format

        /// <summary>
        /// Emits documentation for a single type.
        /// </summary>
        /// <param name="type"></param>
        private void EmitTypeDocumentation(TypeInfo type)
        {
            // the XML structure representing a type
            Writer.WriteStartElement("TypeDocumentation");
            Writer.WriteAttributeString("Name", type.Symbol.Name);
            Writer.WriteAttributeString("Key", type.QualifiedName);

            CurrentTypeName = type.QualifiedName;

            // get declaration ... there can be multiple trees
            TypeDeclarationSyntax decl = null;
            var firstnode = type.Nodes.First();
            if (firstnode is TypeDeclarationSyntax)
            {
                decl = (TypeDeclarationSyntax)firstnode;
                var kind = decl.Kind().ToString();
                var userkind = kind.EndsWith("Declaration") ? kind.Substring(0, kind.Length - 11) : kind;
                Writer.WriteAttributeString("Kind", userkind);
            }
            else
            {
                // TODO: or none at all? no declaration?
            }

            // process all syntax nodes
            foreach (var node in type.Nodes)
            {
                try
                {
                    // load class declaration comment (can be multiple for partial)
                    TryLoadComment(node);

                    // setup semantic model
                    Model = Suction.Compilation.GetSemanticModel(node.SyntaxTree, false);

                    if (null != decl)
                    {
                        // emit relation to namespace container
                        EmitNamespaceComponents(decl, EmitNamespaceComponentLink);

                        // emit declaration section
                        EmitDeclaration(decl);
                        decl = null;
                    }

                    // emit members of this class
                    EmitClassMembers(node);

                    EmitTypeRelations(node.ChildNodes());
                }
                finally
                {
                    Model = null;
                }
            }

            CurrentTypeName = null;

            Writer.WriteEndElement();
        }

        private void EmitTypeRelations(IEnumerable<SyntaxNode> nodelist, TopicRelation kind = null)
        {
            kind = kind ?? new TopicReferences();

            foreach (var node in nodelist)
            {
                if (node is SimpleNameSyntax)
                {
                    ITypeSymbol symbol;
                    if (TryGetTypeSymbol((SimpleNameSyntax)node, out symbol))
                    {
                        AddTopicRelation(symbol.GetFullName(), kind);
                    }
                }
                else if (node is BaseTypeSyntax)
                {
                    kind = new TopicIsDerivedFrom();
                }

                EmitTypeRelations(node.ChildNodes(), kind);
            }
        }

        private bool IsVisible(SyntaxTokenList modifiers)
        {
            /*var visible = false;
            foreach (var m in modifiers)
            {
                if (m.Text == "public" || m.Text == "protected")
                {
                    visible = true;
                }
            }*/

            // return visible;
            return true;
        }

        private void EmitClassMembers(SyntaxNode node)
        {
            foreach (var child in node.ChildNodes())
            {
                if (child is MethodDeclarationSyntax)
                {
                    EmitMethodDocumentation((BaseMethodDeclarationSyntax)child);
                }
                else if (child is ConstructorDeclarationSyntax)
                {
                    EmitMethodDocumentation((BaseMethodDeclarationSyntax)child);
                }
                else if (child is PropertyDeclarationSyntax)
                {
                    EmitPropertyDocumentation((PropertyDeclarationSyntax)child);
                }
                else if (child is FieldDeclarationSyntax)
                {
                    EmitFieldDocumentation((FieldDeclarationSyntax)child);
                }
                else if (child is AttributeListSyntax)
                {
                    EmitAttributesDocumentation((AttributeListSyntax)child);
                }
                else if (child is EventFieldDeclarationSyntax)
                {
                    EmitEventDeclaration((EventFieldDeclarationSyntax)child);
                }
                else if (child is EnumMemberDeclarationSyntax)
                {
                    EmitEnumMemberDeclaration((EnumMemberDeclarationSyntax)child);
                }
                else if (child is ClassDeclarationSyntax)
                {
                    EmitNestedClassDeclaration((ClassDeclarationSyntax)child);
                }
                else if(child is EnumDeclarationSyntax)
                {
                    EmitEnumDeclaration((EnumDeclarationSyntax)child);
                }
                else if (child is BaseListSyntax)
                {
                    // ignore for now
                }
                else if (child is TypeParameterListSyntax)
                {
                    // ignored
                }
                else
                {
                    // TODO: other members
                    //Log.Warning("TODO: member syntax [{0}] not implemented.", child.GetType().Name);
                    Suction.Results.Write(new UnhandledSyntaxDocumentationWarning("member syntax [" + child.GetType().Name + "] documentation not supported."));
                }
            }
        }

        private void EmitEnumDeclaration(EnumDeclarationSyntax node)
        {
            Writer.WriteStartElement("NestedType");

            Writer.WriteAttributeString("Name", node.Identifier.Text);
            Writer.WriteAttributeString("Kind", "enum");

            Writer.WriteEndElement();
        }

        private void EmitNestedClassDeclaration(ClassDeclarationSyntax node)
        {
            Writer.WriteStartElement("NestedType");

            Writer.WriteAttributeString("Name", node.Identifier.Text);
            Writer.WriteAttributeString("Kind", "class");

            EmitClassMembers(node);

            Writer.WriteEndElement();
        }

        private void EmitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
        {
            Writer.WriteStartElement("EnumMember");
            Writer.WriteAttributeString("Name", node.Identifier.Text);

            TryLoadComment(node);
            EmitDeclaration(node);

            Writer.WriteEndElement();
        }

        private void EmitEventDeclaration(EventFieldDeclarationSyntax node)
        {
            if (IsVisible(node.Modifiers))
            {
                foreach (var v in node.Declaration.Variables)
                {
                    Writer.WriteStartElement("Event");
                    Writer.WriteAttributeString("Name", v.Identifier.ToString());
                    Writer.WriteAttributeString("Modifiers", node.Modifiers.ToSeparatorList(" "));
                    TryLoadComment(node);
                    EmitDeclaration(node);

                    EmitTypeDecorated(node.Declaration.Type);

                    Writer.WriteEndElement();
                }
            }
        }

        private void EmitFieldDocumentation(FieldDeclarationSyntax node)
        {
            if (IsVisible(node.Modifiers))
            {
                foreach (var v in node.Declaration.Variables)
                {
                    Writer.WriteStartElement("Field");
                    Writer.WriteAttributeString("Name", v.Identifier.ToString());
                    Writer.WriteAttributeString("Modifiers", node.Modifiers.ToSeparatorList(" "));
                    TryLoadComment(node);
                    EmitDeclaration(node);

                    EmitTypeDecorated(node.Declaration.Type);

                    Writer.WriteEndElement();
                }
            }
        }

        private void EmitPropertyDocumentation(PropertyDeclarationSyntax node)
        {
            if (IsVisible(node.Modifiers))
            {
                Writer.WriteStartElement("Property");
                Writer.WriteAttributeString("Name", node.Identifier.ToString());

                TryLoadComment(node);
                EmitDeclaration(node);

                EmitModifiers(node.Modifiers);

                EmitTypeDecorated(node.Type);

                Writer.WriteEndElement();
            }
        }

        private void EmitModifiers(SyntaxTokenList modifiers)
        {
            foreach (var modifier in modifiers)
            {
                Writer.WriteElementString("Modifier", modifier.ToString());
            }
        }

        private void EmitMethodDocumentation(BaseMethodDeclarationSyntax node)
        {
            if (IsVisible(node.Modifiers))
            {

                string name = CurrentTypeName;

                if (node is MethodDeclarationSyntax)
                {
                    Writer.WriteStartElement("Method");
                    name = ((MethodDeclarationSyntax)node).Identifier.Text;
                }
                else
                {
                    Writer.WriteStartElement("Constructor");
                }

                Writer.WriteAttributeString("Name", name);

                TryLoadComment(node);
                EmitDeclaration(node);

                EmitModifiers(node.Modifiers);

                if (node is MethodDeclarationSyntax)
                {
                    var method = (MethodDeclarationSyntax)node;
                    Writer.WriteStartElement("Return");
                    EmitTypeDecorated(method.ReturnType);
                    Writer.WriteEndElement();
                }

                foreach (var p in node.ParameterList.Parameters)
                {
                    Writer.WriteStartElement("Parameter");
                    Writer.WriteAttributeString("Name", p.Identifier.ValueText);

                    EmitTypeDecorated(p.Type);

                    Writer.WriteEndElement();
                }


                Writer.WriteEndElement();
            }
        }

        private void EmitTypeDecorated(TypeSyntax node)
        {
            ITypeSymbol symbol;
            if (TryGetTypeSymbol(node, out symbol))
            {
                if (node is PredefinedTypeSyntax)
                {
                    Writer.WriteStartElement("Type");
                    Writer.WriteStartElement("Predefined");
                    Writer.WriteAttributeString("Name", node.ToString());
                    Writer.WriteEndElement();
                    Writer.WriteEndElement();
                }
                else if (node is ArrayTypeSyntax)
                {
                    var s = (ArrayTypeSyntax)node;
                    Writer.WriteStartElement("Type");
                    Writer.WriteStartElement("Array");
                    EmitTypeDecorated(s.ElementType);
                    Writer.WriteEndElement();
                    Writer.WriteEndElement();
                }
                else if (node is SimpleNameSyntax)
                {
                    Writer.WriteStartElement("Type");

                    Writer.WriteStartElement("Identifier");
                    Writer.WriteAttributeString("Key", symbol.GetFullName());
                    Writer.WriteAttributeString("Name", symbol.Name);
                    Writer.WriteEndElement();

                    if (node is GenericNameSyntax)
                    {
                        EmitGenericTypeDecorated((GenericNameSyntax)node, symbol);
                    }
                    else if (node is SimpleNameSyntax)
                    {
                        EmitSimpleNameSyntaxDecorated((SimpleNameSyntax)node, symbol);
                    }
                    else
                    {
                        Writer.WriteElementString("XXX", node.GetType().Name);
                    }

                    Writer.WriteEndElement();
                }
            }
        }

        private void EmitSimpleNameSyntaxDecorated(SimpleNameSyntax node, ITypeSymbol symbol)
        {
        }

        private void EmitGenericTypeDecorated(GenericNameSyntax node, ITypeSymbol symbol)
        {
            //Writer.WriteElementString("Name", node.Identifier.ValueText);

            Writer.WriteStartElement("TypeArguments");

            foreach (var typearg in node.TypeArgumentList.Arguments)
            {
                EmitTypeDecorated(typearg);
            }

            Writer.WriteEndElement();
        }

        private void EmitAttributesDocumentation(AttributeListSyntax node)
        {
            foreach (var a in node.Attributes)
            {
                Writer.WriteStartElement("Attribute");
                Writer.WriteAttributeString("Name", a.Name.ToString());

                EmitDeclaration(a);

                Writer.WriteEndElement();
            }
        }

        #region Declarations

        /// <summary>
        /// Emits the 'Declaration' element for a given syntax node.
        /// </summary>
        /// <param name="node">The syntax node to emit.</param>
        private void EmitDeclaration(SyntaxNode node)
        {
            Writer.WriteStartElement("Declaration");
            Writer.WriteStartElement("code", EmitDocumentation.CodeDecorationNamespaceURI);

            WhitespaceNeeded = false;

            EmitSyntaxDeclaration(node);

            Writer.WriteEndElement();
            Writer.WriteEndElement();
        }

        private void EmitSyntaxDeclaration(SyntaxNode node)
        {
            if (node is ClassDeclarationSyntax)
            {
                EmitClassDeclaration((ClassDeclarationSyntax)node);
            }
            else if (node is InterfaceDeclarationSyntax)
            {
                EmitInterfaceDeclaration((InterfaceDeclarationSyntax)node);
            }
            else if (node is MethodDeclarationSyntax)
            {
                EmitMethodDeclaration((MethodDeclarationSyntax)node);
            }
            else if (node is ConstructorDeclarationSyntax)
            {
                EmitConstructorDeclaration((ConstructorDeclarationSyntax)node);
            }
            else if (node is EnumMemberDeclarationSyntax)
            {
                EmitDeclarationEnumMember((EnumMemberDeclarationSyntax)node);
            }
            else if (node is PropertyDeclarationSyntax)
            {
                var decl = (PropertyDeclarationSyntax)node;
                foreach (var modifier in decl.Modifiers)
                {
                    EmitSpan(modifier.Text, "code-keyword");
                }

                EmitSyntaxDeclaration(decl.Type);

                EmitSpan(decl.Identifier.Text, "code-identifier");

                EmitSpan("{");

                foreach (var accessor in decl.AccessorList.Accessors)
                {
                    EmitSyntaxDeclaration(accessor);
                }

                EmitSpan("}");
            }
            else if (node is FieldDeclarationSyntax)
            {
                var field = (FieldDeclarationSyntax)node;
                foreach (var e in field.Declaration.Variables)
                {
                    EmitFieldDeclaration(field, e);
                }
            }
            else if (node is EventFieldDeclarationSyntax)
            {
                var field = (EventFieldDeclarationSyntax)node;
                foreach (var e in field.Declaration.Variables)
                {
                    EmitEventFieldDeclaration(field, e);
                }
            }
            else if (node is TypeSyntax)
            {
                EmitDeclarationTypeReference((TypeSyntax)node);
            }
            else if (node is AccessorDeclarationSyntax)
            {
                var decl = (AccessorDeclarationSyntax)node;
                foreach (var modifier in decl.Modifiers)
                {
                    EmitSpan(modifier.Text, "code-keyword");
                }

                EmitSpan(decl.Keyword.Text, "code-keyword");
                EmitSpan(";", null, false);
            }
            else if (node is ParameterSyntax)
            {
                var decl = (ParameterSyntax)node;
                EmitDeclarationTypeReference(decl.Type);
                EmitSpan(decl.Identifier.Text, "code-parameter-name");
            }
            else if (node is AttributeSyntax)
            {
                EmitDeclarationAttribute((AttributeSyntax)node);
            }
            else
            {
                Suction.Results.Write(new UnhandledSyntaxDocumentationWarning("declaration syntax [" + node.GetType().Name + "] documentation not supported."));

                EmitSpan(node.GetType().Name, "code-syntax-unhandled");
            }
        }

        private void EmitDeclarationAttribute(AttributeSyntax node)
        {
            EmitImplementation(node);
        }

        private void EmitModifierDeclaration(SyntaxTokenList modifiers)
        {
            foreach (var modifier in modifiers)
            {
                EmitSpan(modifier.Text, "code-keyword");
            }
        }

        /// <summary>
        /// Generates a namespace navigation component.
        /// </summary>
        private void EmitDeclarationNamespaceLink(string name, IEnumerable<string> elements, bool innermost)
        {
            Writer.WriteStartElement("a");
            Writer.WriteAttributeString("href", elements.ToSeparatorList("."));
            Writer.WriteString(name);
            Writer.WriteEndElement();

            Writer.WriteString(".");
            WhitespaceNeeded = false;
        }

        private void EmitBaseListSyntaxDeclaration(BaseListSyntax baselist)
        {
            if (null != baselist)
            {
                EmitSpan(":");
                EmitSpaceBefore();

                EmitSeparatedList(baselist.Types, e => EmitDeclarationTypeReference(e.Type));
            }
        }

        private void EmitInterfaceDeclaration(InterfaceDeclarationSyntax decl)
        {
            // modifiers keyword
            EmitModifierDeclaration(decl.Modifiers);

            // 'interface' keyword
            EmitSpan(decl.Keyword.Text, "code-keyword");

            EmitSpaceBefore();

            // containing namespaces (for navigation)
            EmitNamespaceComponents(decl, EmitDeclarationNamespaceLink);

            // identifier
            EmitSpan(decl.Identifier.Text, "code-identifier");

            EmitBaseListSyntaxDeclaration(decl.BaseList);

            EmitSpan("{ ... }");
        }

        private void EmitClassDeclaration(ClassDeclarationSyntax decl)
        {
            EmitModifierDeclaration(decl.Modifiers);
            EmitSpan(decl.Keyword.Text, "code-keyword");

            EmitSpaceBefore();
            EmitNamespaceComponents(decl, EmitDeclarationNamespaceLink);
            EmitSpan(decl.Identifier.Text, "code-identifier");

            EmitBaseListSyntaxDeclaration(decl.BaseList);

            EmitSpan("{ ... }");
        }

        private void EmitFieldDeclaration(FieldDeclarationSyntax decl, VariableDeclaratorSyntax v)
        {
            foreach (var modifier in decl.Modifiers)
            {
                EmitSpan(modifier.Text, "code-keyword");
            }

            EmitSyntaxDeclaration(decl.Declaration.Type);

            EmitSpan(v.Identifier.Text, "code-identifier");
        }

        private void EmitEventFieldDeclaration(EventFieldDeclarationSyntax decl, VariableDeclaratorSyntax v)
        {
            foreach (var modifier in decl.Modifiers)
            {
                EmitSpan(modifier.Text, "code-keyword");
            }

            EmitSpan("event", "code-keyword");
            EmitSyntaxDeclaration(decl.Declaration.Type);

            EmitSpan(v.Identifier.Text, "code-identifier");
        }

        private void EmitMethodDeclaration(MethodDeclarationSyntax decl)
        {
            Writer.WriteStartElement("header");

            foreach (var modifier in decl.Modifiers)
            {
                EmitSpan(modifier.Text, "code-keyword");
            }

            EmitSyntaxDeclaration(decl.ReturnType);

            EmitSpan(decl.Identifier.Text, "code-identifier");

            EmitSpan("(", null, false);
            var first = true;
            foreach (var p in decl.ParameterList.Parameters)
            {
                if (first) { first = false; WhitespaceNeeded = false; }
                else EmitSpan(",", null, false);
                EmitSyntaxDeclaration(p);
            }

            EmitSpan(")", null, false);

            Writer.WriteEndElement();

            EmitImplementation(decl.Body);
        }

        private void EmitConstructorDeclaration(ConstructorDeclarationSyntax decl)
        {
            Writer.WriteStartElement("header");

            foreach (var modifier in decl.Modifiers)
            {
                EmitSpan(modifier.Text, "code-keyword");
            }

            EmitSpan(decl.Identifier.Text, "code-identifier");

            EmitSpan("(", null, false);
            var first = true;
            foreach (var p in decl.ParameterList.Parameters)
            {
                if (first) { first = false; WhitespaceNeeded = false; }
                else EmitSpan(",", null, false);
                EmitSyntaxDeclaration(p);
            }

            EmitSpan(")", null, false);

            Writer.WriteEndElement();

            EmitImplementation(decl.Body);
        }

        private void EmitTrivia(SyntaxTrivia trivia)
        {
            if (trivia.Kind() == SyntaxKind.SingleLineCommentTrivia)
            {
                Writer.WriteStartElement("span");
                Writer.WriteAttributeString("class", "code-comment");
                EmitImplementationCode(trivia.ToFullString());
                Writer.WriteEndElement();
            }
            else
            {
                EmitImplementationCode(trivia.ToFullString());
            }
        }

        private void EmitDeclarationEnumMember(EnumMemberDeclarationSyntax decl)
        {
            EmitSpan(decl.Identifier.Text, "code-identifier");

            if (null != decl.EqualsValue)
            {
                EmitImplementationRecurse(decl.EqualsValue);
            }
        }

        #endregion

        #region Implementation Code

        /// <summary>
        /// Performs deindentation of preformatted code.
        /// </summary>
        /// <param name="s"></param>
        private void EmitImplementationCode(string s)
        { 
            var b = new StringBuilder();

            foreach (var c in s)
            {
                var copy = false;
                switch (_tstate)
                {
                    case TriviaState.initial:
                        switch (c)
                        {
                            case ' ':
                                _tindent++;
                                break;

                            case '\r':
                            case '\n':
                                _tindent = 0;
                                break;

                            case '\t':
                                _tindent += 4;
                                break;

                            default:
                                // deindent count established, start of copy ...
                                _tstate = TriviaState.start;
                                break;
                        }
                        break;

                    case TriviaState.start:
                        switch (c)
                        {
                            case '\r':
                            case '\n':
                                _tstate = TriviaState.line;
                                _teat = 0;
                                copy = true;
                                break;
                        }
                        break;

                    case TriviaState.line:
                        switch (c)
                        {
                            case ' ':
                                _teat++;
                                break;

                            case '\t':
                                _teat += 4;
                                break;

                            case '\r':
                            case '\n':
                                _teat = 0;
                                copy = true;
                                break;

                            default:
                                // buggy indent?
                                _tstate = TriviaState.start;
                                break;
                        }

                        if (_teat > _tindent)
                        {
                            _tstate = TriviaState.start;
                        }
                        break;
                }

                if (_tstate == TriviaState.start)
                {
                    copy = true;
                }

                if(copy)
                { 
                    b.Append(c);
                }
            }

            Writer.WriteString(b.ToString());
        }

        /// <summary>
        /// Emits a source code block for a syntax node.
        /// </summary>
        /// <param name="parent">The node to emit.</param>
        private void EmitImplementation(SyntaxNode parent)
        {
            if (null != parent)
            {
                Writer.WriteStartElement("implementation");
                _tstate = TriviaState.initial;
                _tindent = 0;

                EmitImplementationRecurse(parent);
                Writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Recurses source code output.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="depth"></param>
        private void EmitImplementationRecurse(SyntaxNode parent, int depth = 0)
        {
            foreach (var e in parent.ChildNodesAndTokens())
            {
                if (e.IsNode)
                {
                    // just recurse into child nodes
                    EmitImplementationRecurse(e.AsNode(), depth + 1);
                }
                else if (e.IsToken)
                {
                    var token = e.AsToken();

                    // emit leading trivia
                    foreach (var trivia in token.LeadingTrivia)
                    {
                        EmitTrivia(trivia);
                    }

                    // lookup symbol info ...
                    var makeref = false;
                    var info = Model.GetSymbolInfo(parent);
                    if (null != info.Symbol)
                    {
                        switch (info.Symbol.Kind)
                        {
                            case SymbolKind.NamedType:
                                makeref = true;
                                break;

                            default:
                                // TODO: handle other kinds of symbols
                                break;
                        }
                    }

                    // make a link if appropriate ...
                    var bracket = false;
                    if (makeref && parent is TypeSyntax)
                    {
                        ITypeSymbol symbol;
                        if (TryGetTypeSymbol((TypeSyntax)parent, out symbol))
                        {
                            Writer.WriteStartElement("a");
                            Writer.WriteAttributeString("href", symbol.GetFullName());
                            bracket = true;
                        }
                    }

                    // emit the token
                    EmitImplementationCode(token.Text);

                    if (bracket)
                    {
                        Writer.WriteEndElement();
                    }

                    // trailing trivia
                    foreach (var trivia in token.TrailingTrivia)
                    {
                        EmitTrivia(trivia);
                    }
                }
            }


        }

        #endregion

        #region Types References

        private bool TryGetTypeSymbol(SyntaxNode node, out ITypeSymbol symbol)
        {
            symbol = null;

            var info = Model.GetSymbolInfo(node);
            if(null == info.Symbol)
            {
                Trace("[TryGetTypeSymbol] no symbol information for '{0}'.", node);
                return false;
            }

            if(!(info.Symbol is ITypeSymbol))
            {
                Trace("[TryGetTypeSymbol] not a type symbol '{0}', kind {1}.", node, info.Symbol.Kind);
                return false;
            }

            symbol = (ITypeSymbol)info.Symbol;
            return true;
        }

        private void EmitDeclarationTypeReference(TypeSyntax node)
        {
            // resolve symbol for TypeInfo ...
            var typeinfo = Model.GetTypeInfo(node);
            if (null == typeinfo.Type)
            {
                throw new Exception("unresolved type in '" + node + "'.");
            }

            ITypeSymbol symbol = typeinfo.Type;
            if (node is PredefinedTypeSyntax)
            {
                EmitSpan(node.ToString(), "code-type-predefined");
            }
            else if (node is ArrayTypeSyntax)
            {
                var s = (ArrayTypeSyntax)node;
                EmitDeclarationTypeReference(s.ElementType);
                EmitSpan("[]", null, false);
            }
            else if (node is SimpleNameSyntax)
            {
                EmitSpaceBefore();
                Writer.WriteStartElement("a");

                Writer.WriteAttributeString("href", symbol.GetFullName());
                Writer.WriteString(symbol.Name);
                Writer.WriteEndElement();
                WhitespaceNeeded = true;

                if (node is GenericNameSyntax)
                {
                    EmitSpan("<", null, false);
                    var name = (GenericNameSyntax)node;
                    var first = true;
                    foreach (var typearg in name.TypeArgumentList.Arguments)
                    {
                        if (first) { first = false; WhitespaceNeeded = false; }
                        else EmitSpan(",", null, false);
                        EmitDeclarationTypeReference(typearg);
                    }

                    EmitSpan(">", null, false);
                }
                /*else if (node is SimpleNameSyntax)
                {
                    EmitSimpleNameSyntaxDecorated((SimpleNameSyntax)node, symbol);
                }
                else
                {
                    Writer.WriteElementString("XXX", node.GetType().Name);
                }*/
            }
        }

        #endregion

        #region Primitives

        private void EmitSpaceBefore()
        {
            if (WhitespaceNeeded)
            {
                //Writer.WriteElementString("span", "\x20");
                Writer.WriteStartElement("span");
                Writer.WriteWhitespace(" ");
                Writer.WriteEndElement();
                WhitespaceNeeded = false;
            }
        }

        private void EmitLineBreak()
        {
            Writer.WriteStartElement("br");
            Writer.WriteEndElement();
        }

        private void EmitSpan(string text, string css = null, bool wsbefore = true)
        {
            if (wsbefore) EmitSpaceBefore();

            Writer.WriteStartElement("span");
            if (null != css) Writer.WriteAttributeString("class", css);
            Writer.WriteString(text);
            Writer.WriteEndElement();

            WhitespaceNeeded = true;
        }

        private void EmitSeparatedList<T>(IEnumerable<T> nodes, Action<T> action)
        {
            var first = true;
            foreach (var node in nodes)
            {
                if (first) { first = false; WhitespaceNeeded = false; }
                else EmitSpan(",", null, false);
                action(node);
            }
        }

        #endregion

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

        private bool TryLoadComment(SyntaxNode node)
        {
            var text = node.GetLeadingTrivia().ToString();
            var reader = new StringReader(text);
            string line;

            var sb = new StringBuilder();

            while (null != (line = reader.ReadLine()))
            {
                line = line.Trim();
                if (line.StartsWith("///"))
                {
                    line = line.Substring(3).Trim();
                    if (!string.IsNullOrEmpty(line))
                    {
                        if (sb.Length > 0) sb.Append(" ");
                        sb.Append(line);
                    }
                }
            }

            if (sb.Length > 0)
            {
                // Program.Trace("comment: {0}", sb.ToString());

                try
                {
                    var xml = "<comment xmlns='ms:csdoc'>" + sb.ToString() + "</comment>";
                    var doc = new XmlDocument();
                    doc.LoadXml(xml);

                    Writer.WriteStartElement("Annotation");
                    doc.DocumentElement.WriteContentTo(Writer);
                    Writer.WriteEndElement();

                    return true;
                }
                catch (Exception ex)
                {
                    Log.Warning("error loading comment from {0}: {1}", node.SyntaxTree.FilePath, ex.Message);
                }
            }

            return false;
        }

        #endregion
    }
}
