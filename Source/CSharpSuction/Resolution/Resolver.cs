using Common;
using CSharpSuction.Input;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSuction.Resolution
{
    /// <summary>
    /// Resolves identifiers to symbols for a single source file.
    /// </summary>
    class Resolver
    {
        #region Privates

        private Suction _suction;
        private SourceInfo _source;
        private SemanticModel _model;
        private int _stage;
        private HashSet<string> _unresolvednames = new HashSet<string>();
        private HashSet<string> _resolved = new HashSet<string>();

        /// <summary>
        /// Number of symbols process in the current loop.
        /// </summary>
        private int _symbolcount;

        private static ResolverMethodMap _map = new ResolverMethodMap();

        private enum State { top, member, initializer, type };
        private Stack<State> _statestack = new Stack<State>();

        #endregion

        #region Properties

        public SourceInfo Source { get { return _source; } }

        public bool IsCompleted { get; private set; }

        #endregion

        #region Construction

        public Resolver(Suction suction, SourceInfo source)
        {
            _suction = suction;
            _source = source;

            Trace("resolve '{0}' created ...", source.FullPath);
        }

        #endregion

        #region Diagnostics

        public void Trace(string format, params object[] args)
        {
            if (_suction.ShowResolve)
            {
                _suction.Results.Debug(format, args);
            }
        }

        public void TraceResolve(string format, params object[] args)
        {
            if (_suction.ShowResolve)
            {
                _suction.Results.Debug(format, args);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Calles repeatedly until the source reaches a conclusive state.
        /// </summary>
        public void Resolve()
        {
            try
            {
                Source.Resolving();

                _unresolvednames.Clear();

                // get tree and root
                var tree = Source.Tree;
                var root = tree.GetRoot();

                // need the semantic model
                _model = _suction.Compilation.GetSemanticModel(tree, false);

                // install state stack
                _statestack.Push(State.top);

                // TODO: loop until?
                bool loop;
                do
                {
                    loop = false;
                    _symbolcount = 0;
                    var s = _stage++;
                    if (s == 0)
                    {
                        ProcessUnresolvedBaseClasses(root);

                        loop = 0 == _symbolcount;
                    }
                    else
                    {
                        ProcessUnresolvedSymbols(root);
                    }
                }
                while (loop);

                if (0 == _symbolcount)
                {
                    // no symbols processed in this pass ...
                    if (_unresolvednames.Any())
                    {
                        // TODO: accurate unresolved infos??
                        //Log.Warning("{0}: unresolved names remain: {1}", Source.FullPath, _unresolvednames.ToSeparatorList());
                        //SetFailed();
                    }

                    // nothing new
                    SetCompleted(root);
                }
                else
                {
                    // Log.Debug("{0}: {1} symbols were processed, {2} unresolved: {3}", Source.FullPath, _symbolcount, _unresolvednames.Count, _unresolvednames.ToSeparatorList());
                }
            }
            finally
            {
                _model = null;
            }
        }

        #endregion

        #region Private Methods

        private void ProcessUnresolvedBaseClasses(SyntaxNode node)
        {
            foreach (var us in node.DescendantNodes().OfType<UsingDirectiveSyntax>())
            {
                // trigger referencing of assemblies
                _suction.ReportUsingDirective(us);
            }

            // process baseclasses first
            foreach (var bc in node.DescendantNodes().OfType<BaseListSyntax>())
            {
                ProcessUnresolvedSymbols(bc);
            }
        }

        #region Process Unresolved

        private void ProcessUnresolvedSymbols(SyntaxNode node)
        {
            var method = _map.GetSyntaxNodeMethod("ProcessUnresolved", node);

            // Log.Debug("calling {0} with {1} ...", method.Name, node.GetType().Name);

            method.Invoke(this, new object[] { node });
        }

        private void ProcessUnresolvedSyntaxNode(SyntaxNode node)
        {
            // Log.Debug("[ProcessUnresolvedSyntaxNode] {0}\n{1}",node.GetType().Name, node);
            foreach (var child in node.ChildNodes())
            {
                ProcessUnresolvedSymbols(child);
            }
        }

        private void ProcessUnresolvedAttributeSyntax(AttributeSyntax node)
        {
            ResolveName(node);

            if (null != node.ArgumentList)
            {
                foreach (var e in node.ArgumentList.Arguments)
                {
                    ProcessUnresolvedSymbols(e);
                }
            }
        }

        private void ProcessUnresolvedUsingDirectiveSyntax(UsingDirectiveSyntax node)
        {
            // ignore
        }

        private void ProcessUnresolvedQualifiedNameSyntax(QualifiedNameSyntax node)
        {
            ResolveName(node);
        }

        private bool ProcessUnresolvedSimpleNameSyntax(SimpleNameSyntax node)
        {
            if (!node.IsVar)
            {
                if (!ResolveName(node))
                {
                    return false;
                }
            }

            if (node is GenericNameSyntax)
            {
                var gname = (GenericNameSyntax)node;

                try
                {
                    _statestack.Push(State.top);
                    ProcessUnresolvedSymbols(gname.TypeArgumentList);
                }
                finally
                {
                    _statestack.Pop();
                }
            }
            else
            {

            }

            return true;
        }

        /*private void ProcessUnresolvedObjectCreationExpressionSyntax(ObjectCreationExpressionSyntax node)
        {
            ProcessUnresolvedSymbols(node.Type);
        }*/

        private bool ProcessUnresolvedMemberAccessExpressionSyntax(SyntaxNode node)
        {
            var p = (MemberAccessExpressionSyntax)node;
            var expression = p.Expression;

            // resolve left side
            ProcessUnresolvedSymbols(expression);

            // is there a symbol.
            var left = _model.GetSymbolInfo(expression);
            if (null == left.Symbol)
            {
                // Log.Debug("left side of '{0}' is unresolved.", node);
                AddToUnresolvedNames(expression.ToString());
                return false;
            }

            var rstate = State.top;

            switch (left.Symbol.Kind)
            {
                case SymbolKind.Local:
                case SymbolKind.Field:
                case SymbolKind.Parameter:
                case SymbolKind.RangeVariable:
                case SymbolKind.Method:
                case SymbolKind.Property:
                    rstate = State.member;
                    break;

                case SymbolKind.NamedType:
                case SymbolKind.TypeParameter:
                    rstate = State.type;
                    break;
            }

            try
            {
                // processing options to stack
                _statestack.Push(rstate);

                ProcessUnresolvedSymbols(p.Name);


                /*if (!ProcessUnresolvedSimpleNameSyntax(p.Name))
                {
                    if (left.Symbol.Kind == SymbolKind.Namespace)
                    {
                        // simple name was not resolve: hoo?
                        //if (IsNamespaceIdentifier(p))
                        {
                            // try parent
                            foreach (var ancestor in p.Ancestors().OfType<MemberAccessExpressionSyntax>())
                            {
                                var namelist = ancestor.ConvertName();
                                Trace("try {0} => {1}", ancestor, namelist.ToSeparatorList());
                                var sinfo = _model.GetSymbolInfo(ancestor);
                                if (ResolveSymbolName(namelist, sinfo, ancestor))
                                {
                                    break;
                                }
                            }
                        }
                    }
                }*/

                // expression was resolved, resolve name (can be generic)
                /*if (ProcessUnresolvedSymbols(p.Name))
                {
                }*/
            }
            finally
            {
                _statestack.Pop();
            }

            return false;
        }

        #endregion

        private bool IsNamespaceIdentifier(MemberAccessExpressionSyntax m)
        {
            if(m.Expression is MemberAccessExpressionSyntax)
            {
                return IsNamespaceIdentifier((MemberAccessExpressionSyntax)m.Expression);
            }
            else if(m.Expression is SimpleNameSyntax)
            {
                var sinfo = _model.GetSymbolInfo(m.Expression);
                var result = sinfo.Symbol.Kind == SymbolKind.Namespace;

                return result;
            }
            else
            {
                return false;
            }
        }

        private bool ResolveName(SyntaxNode node)
        {
            if (_statestack.Peek() == State.type)
            {
                // don't do resolution to the right of a type identifier.
                return true;
            }

            var namelist = node.ConvertName();

            // Trace("[ResolveName] {0} ...", namelist.ToSeparatorList("."));

            var sinfo = _model.GetSymbolInfo(node);
            if (null != sinfo.Symbol)
            {
                // symbol was already resolved
                return true;
            }

            var name = namelist.Last();

            if (!AddToUnresolvedNames(name))
            {
                return true;
            }

            if (!ResolveSymbolName(namelist, sinfo, node))
            {
                Trace("name {0,-30} stage {1} ==> unresolved", name, _stage);
                return false;
            }
            else
            {
                Trace("name {0,-30} stage {1} ==> OK", name, _stage);
                return true;
            }
        }

        private bool AddToUnresolvedNames(string name)
        {
            // add to unresolved names
            return _unresolvednames.Add(name);
        }

        private HashSet<string> GetUsings(SyntaxNode node)
        {
            var usings = new HashSet<string>();
            foreach (var nsnode in node.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>())
            {
                var parts = nsnode.Name.ToString().Split('.');
                for (int j = parts.Length; j > 0; --j)
                {
                    var ns = parts.Take(j).ToSeparatorList(".");
                    usings.Add(string.Intern(ns));
                }
            }


            foreach (var x in node.Ancestors().OfType<CompilationUnitSyntax>().First().ChildNodes().OfType<UsingDirectiveSyntax>())
            {
                usings.Add(string.Intern(x.Name.ToString()));
            }

            return usings;
        }

        /// <summary>
        /// Tries to resolve a possibly qualified name.
        /// </summary>
        /// <param name="namelist"></param>
        /// <param name="sinfo"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool ResolveSymbolName(string[] namelist, SymbolInfo sinfo, SyntaxNode node)
        {
            var name = namelist.Last();

            // TraceResolve("seaching unresolved name '{0}' in the context of '{1}' stage {2} ...", name, node.GetDeclaration(), _stage);

            // result of symbol resolution
            IEnumerable<INameInfo> result = new INameInfo[0];

            // get candidates for the specified name
            var candidates = _suction.LookupName(name);
            if (_statestack.Peek() == State.member)
            {
                // looking for a member ...
                var leftype = GetLeftTypeOfExtensionCall(node);

                if (null == leftype || leftype is IErrorTypeSymbol)
                {
                    // left side was not resolved; no candidates silently
                    candidates = candidates.Where(c => false);
                }
                else
                {
                    // do extension method resolution
                    var list = new List<INameInfo>();
                    var extmeths = candidates.Where(c => c.IsExtensionMethod);

                    foreach (var c in extmeths)
                    {
                        var m = (MethodDeclarationSyntax)((NameInfo)c).Node;

                        var argpar = m.ParameterList.Parameters.First();

                        // TODO: compares type in a 'rough' way, because extension method is not yet part of compilation!
                        if (AreExtensionTypesCompatible(leftype, argpar.Type))
                        {
                            list.Add(c);
                        }
                    }

                    candidates = list.AsEnumerable();

                    if (extmeths.Any() && !candidates.Any())
                    {
                        // Log.Warning("no extension method matched for [{0}].[{1}].", leftype.Name, name);
                    }
                }
            }
            else
            {
                candidates = candidates.Where(c => c.IsTypeName);
            }

            var count = candidates.Count();
            if (count == 1)
            {
                var usings = GetUsings(node);

                // filter by namespace, if any
                result = candidates.Where(e => e.Namespace == null || usings.Any(u => e.Namespace.StartsWith(u)));
            }
            else if(count > 1)
            {
                // there are multiple candidates for the last name
                var fullname = namelist.ToSeparatorList(".");

                var sbdiag = new StringBuilder();
                sbdiag.AppendFormat("multiple matches for '{0}': ...", fullname);
                sbdiag.AppendLine();

                if (namelist.Length > 1)
                {
                    candidates = FilterCandidates(candidates, namelist);
                }

                foreach (var c in candidates)
                {
                    sbdiag.AppendLine("  " + c.QualifiedName.PadRight(40) + " " + c.Source.FullPath);
                }

                sbdiag.AppendLine("namespace:");
                var usings = GetUsings(node);

                // collect declaration namespaces and using declarations of current tree

                foreach(var us in usings)
                {
                    sbdiag.AppendLine("  using " + us);
                }

                var matches = new Dictionary<string, List<INameInfo>>();

                // enumerate the candidates, check namespace and compare fullname
                foreach (var c in candidates)
                {
                    if (c.QualifiedName == fullname)
                    {
                        AddMatch(matches, c.QualifiedName, c);
                    }
                    else
                    {
                        var ns = c.Namespace;
                        var ncount = namelist.Count();
                        if (ncount > 1)
                        {
                            ncount--;
                            var match = true;

                            // trim trailing names
                            var nsparts = ns.Split('.');
                            var scount = nsparts.Count();

                            while (ncount > 0 && scount > 0)
                            {
                                ncount--;
                                scount--;
                                if (nsparts[scount] != namelist[ncount])
                                {
                                    match = false;
                                    break;
                                }
                            }

                            if (!match)
                            {
                                continue;
                            }

                            ns = nsparts.Take(scount).ToSeparatorList(".");
                        }

                        var dname = c.QualifiedName;

                        sbdiag.AppendLine("   check " + dname);

                        if (usings.Contains(ns))
                        {
                            if (dname.EndsWith(fullname))
                            {
                                sbdiag.AppendLine("    match " + dname);

                                AddMatch(matches, dname, c);
                            }
                        }
                    }
                }

                if (matches.Count == 0)
                {
                    TraceResolve("no match output: {0}", sbdiag);

                    // name was not resolved in any namespace
                    ReportUnresolvedName(name, node);
                }
                else if (matches.Count == 1)
                {
                    result = matches.First().Value;
                }
                else
                {
                    TraceResolve("multiple match output: {0}", sbdiag);

                    Trace("resolver problem: multiple candidates for name '{0}'.", namelist.ToSeparatorList("."));

                    foreach (var lookns in usings)
                    {
                        Trace("  caller namespace '{0}", lookns);
                    }

                    /*foreach (var match in matches)
                    {
                        Trace("  candidate {0}", match.Key, match.Value.Select(n => n.GetDeclaration()));
                    }*/

                    var loc = node.GetLocation().GetLineSpan();
                    Trace("{0}({1})", loc.Path, loc.StartLinePosition.Line + 1);
                }
            }

            if (result.Any())
            {
                AddUnresolvedProcessingResolution(name, result, node);
                return true;
            }
            else
            {
                return false;
            }
        }

        #region Extension Method Matching

        private ITypeSymbol GetLeftTypeOfExtensionCall(SyntaxNode node)
        {
            node = node.Parent;

            ITypeSymbol result = null;
            if (node is MemberAccessExpressionSyntax)
            {
                var max = (MemberAccessExpressionSyntax)node;
                var usim = _model.GetSymbolInfo(max.Expression);
                if (null != usim.Symbol)
                {
                    // type of left
                    var s = usim.Symbol;

                    /*if (s is IPropertySymbol)
                    {
                        result = ((IPropertySymbol)s).Type;
                    }
                    else if (s is ILocalSymbol)
                    {
                        result = ((ILocalSymbol)s).Type;
                    }
                    else if (s is IFieldSymbol)
                    {
                        result = ((IFieldSymbol)s).Type;
                    }
                    else if (s is IParameterSymbol)
                    {
                        result = ((IParameterSymbol)s).Type;
                    }
                    else if (s is IMethodSymbol)
                    {
                        result = ((IMethodSymbol)s).ReturnType;
                    }
                    else
                    {
                        Log.Warning("not handled left kind {0} of [{1}]", s.Kind, s);
                    }*/

                    result = s.GetTypeSymbol();
                }
            }

            return result;
        }

        private string MakeComparableTypeName(ITypeSymbol type)
        {
            if (type is IArrayTypeSymbol)
            {
                return MakeComparableTypeName(((IArrayTypeSymbol)type).ElementType) + "[]";
            }
            else
            {
                return type.Name;
            }
        }

        private IEnumerable<ITypeSymbol> GetTypeElements(ITypeSymbol argument)
        {
            yield return argument;

            foreach(var itf in argument.Interfaces)
            {
                yield return itf;
            }

            if (null != argument.BaseType)
            {
                foreach(var b in GetTypeElements(argument.BaseType))
                {
                    yield return b;
                }
            }
        }

        private bool AreExtensionTypesCompatible(ITypeSymbol leftype, TypeSyntax type)
        {
            bool result = false;
            string rname;
            if (type is GenericNameSyntax)
            {
                rname = ((GenericNameSyntax)type).Identifier.Text;
            }
            else
            {
                rname = type.ToString();
            }

            foreach(var querytype in GetTypeElements(leftype))
            { 
                var lname = MakeComparableTypeName(querytype);

                // TODO: wuah!
                result = string.Equals(lname, rname, StringComparison.InvariantCultureIgnoreCase);

                // Log.Debug("   roughcompare [{0}] ~ [{1}] => {2}", lname, rname, result);

                if (result)
                {
                    break;
                }
            }
            return result;
        }

        #endregion

        private IEnumerable<INameInfo> FilterCandidates(IEnumerable<INameInfo> clist, string[] namelist)
        {
            var qname = namelist.ToSeparatorList(".");
            foreach (var c in clist)
            {
                if(c.QualifiedName == qname || c.QualifiedName.EndsWith("." + qname))
                {
                    yield return c;
                }
            }
        }

        private void AddMatch(IDictionary<string, List<INameInfo>> matches, string dname, INameInfo c)
        {
            List<INameInfo> names;
            if (!matches.TryGetValue(dname, out names))
            {
                matches[dname] = names = new List<INameInfo>();
            }

            names.Add(c);
        }

        /// <summary>
        /// Resolves a name to a sources set.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <param name="node"></param>
        private void AddUnresolvedProcessingResolution(string name, IEnumerable<INameInfo> result, SyntaxNode node)
        {
            if (_resolved.Add(name))
            {
                _symbolcount++;
            }

            if (_suction.ShowResolve)
            {
                var loc = node.GetLocation().GetLineSpan();
                var sourceloc = loc.Path + "(" + (loc.StartLinePosition.Line + 1) + ")";

                TraceResolve("{0}: resolution: {1} --> {2}", sourceloc, name, result.Select(e => e.QualifiedName).ToSeparatorList());
            }

            foreach (var info in result)
            {
                info.Source.Schedule();
            }
        }

        private void ReportUnresolvedName(string name, SyntaxNode node)
        {
            // Trace("TODO: unresolved name {0}", name);
        }

        private void SetCompleted(SyntaxNode root)
        {
            foreach(var decl in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            {
                var stype = _model.GetDeclaredSymbol(decl);
                _suction.AddTypeSource(stype, Source, decl);
            }

            IsCompleted = true;
            Source.Resolved();
        }

        private void SetFailed()
        {
            IsCompleted = true;
            Source.Unresolved();
        }

        #endregion
    }
}
