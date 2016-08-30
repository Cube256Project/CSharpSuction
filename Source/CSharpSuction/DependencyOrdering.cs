using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Common;

namespace CSharpSuction
{
    public class DependencyOrdering
    {
        #region Private

        private class Entry
        {
            public ITypeInfo Type;

            public HashSet<string> DependOn = new HashSet<string>();

            public string Key { get { return Type.QualifiedName; } }
        }

        private Dictionary<string, Entry> _names = new Dictionary<string, Entry>();

        #endregion

        private Suction Suction { get; set; }

        private Entry Current { get; set; }

        private SemanticModel Model { get; set; }

        public IEnumerable<ITypeInfo> Order(Suction suction, IEnumerable<ITypeInfo> types)
        {
            Suction = suction;
            foreach (var type in types)
            {
                var entry = new Entry { Type = type };
                _names[type.QualifiedName] = entry;
            }

            // register dependencies given by declarations ...
            foreach (var entry in _names.Values)
            {
                RegisterDeclarationDependencies(entry);
            }

            while (true)
            {
                // take all entries that do not depend on anything ...
                var list = new HashSet<string>();
                foreach (var entry in _names.Values.Where(e => !e.DependOn.Any()).ToList())
                {
                    list.Add(entry.Key);
                    _names.Remove(entry.Key);

                    yield return entry.Type;
                }

                if (!_names.Any())
                {
                    // no more types to process, done
                    break;
                }

                if (!list.Any())
                {
                    // types to process but none of them is independent ...
                    foreach (var e in _names.OrderByDescending(e => e.Value.DependOn.Count).Take(5))
                    {
                        Log.Debug("{0}", e.Key);
                        foreach (var u in e.Value.DependOn)
                        {
                            Log.Debug("  --> {0}", u);
                        }
                    }

                    throw new Exception("circular dependency or missing type.");
                }

                foreach (var name in list)
                {
                    foreach (var entry in _names.Values)
                    {
                        entry.DependOn.Remove(name);
                    }
                }
            }
        }

        private void RegisterDeclarationDependencies(Entry entry)
        {
            try
            {
                Current = entry;
                foreach (var node in Current.Type.Nodes())
                {
                    Model = Suction.Compilation.GetSemanticModel(node.SyntaxTree, false);

                    RegisterDeclarationDependencies(node);
                }
            }
            finally
            {
                Current = null;
            }
        }

        private void RegisterDeclarationDependencies(SyntaxNode node)
        {
            if (node is ClassDeclarationSyntax)
            {
                RegisterDeclarationDependencies(((ClassDeclarationSyntax)node).BaseList);
            }
            else if (node is InterfaceDeclarationSyntax)
            {
                RegisterDeclarationDependencies(((InterfaceDeclarationSyntax)node).BaseList);
            }
            else if (node is BaseListSyntax)
            {
                var baselist = (BaseListSyntax)node;

                foreach (var type in baselist.Types)
                {
                    var info = Model.GetSymbolInfo(type.Type);
                    if (info.Symbol is ITypeSymbol)
                    {
                        var typeinfo = (ITypeSymbol)info.Symbol;
                        var fullname = typeinfo.GetFullName();

                        if (!fullname.StartsWith("System"))
                        {

                            //Log.Debug("{0} -> {1}", Current.Type.QualifiedName, fullname);

                            Current.DependOn.Add(fullname);
                        }
                    }
                }
            }
        }
    }
}
