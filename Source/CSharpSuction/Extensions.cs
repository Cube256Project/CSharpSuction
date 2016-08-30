using Common;
using CSharpSuction.Input;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Text;

namespace CSharpSuction
{
    public static class Extensions
    {
        public static string ToSeparatorList<T>(this IEnumerable<T> list, string separator = null)
        {
            separator = separator ?? ", ";
            var sb = new StringBuilder();
            foreach(var e in list)
            {
                if (sb.Length > 0) sb.Append(separator);
                sb.Append(e);
            }

            return sb.ToString();
        }

        public static string GetNamespace(this SyntaxNode node)
        {
            string name = null;
            foreach (var ancestor in node.AncestorsAndSelf())
            {
                if (ancestor is NamespaceDeclarationSyntax)
                {
                    var ns = ((NamespaceDeclarationSyntax)ancestor).Name.ToString();
                    if (null == name)
                    {
                        name = ns;
                    }
                    else
                    {
                        name = ns + "." + name;
                    }
                }
            }

            return null == name ? null : string.Intern(name);
        }

        public static string GetDeclaration(this SyntaxNode node)
        {
            string name = null;
            foreach (var ancestor in node.AncestorsAndSelf())
            {
                if (ancestor is MethodDeclarationSyntax)
                {
                    var m = (MethodDeclarationSyntax)ancestor;
                    if (null == name)
                    {
                        name = m.Identifier.Text;

                        if(null != m.TypeParameterList)
                        {
                            name += "`" + m.TypeParameterList.Parameters.Count;
                        }
                    }
                }
                else if (ancestor is TypeDeclarationSyntax)
                {
                    var decl = (TypeDeclarationSyntax)ancestor;
                    var typename = decl.Identifier.Text;
                    if (null == name)
                    {
                        name = typename;
                    }
                    else
                    {
                        name = typename + "#" + name;
                    }

                    if (null != decl.TypeParameterList)
                    {
                        name += "`" + decl.TypeParameterList.Parameters.Count;
                    }

                }
                else if (ancestor is EnumDeclarationSyntax)
                {
                    name = ((EnumDeclarationSyntax)ancestor).Identifier.Text;
                }
                else if (ancestor is NamespaceDeclarationSyntax)
                {
                    if (null != name)
                    {
                        var ns = ((NamespaceDeclarationSyntax)ancestor).Name.ToString();
                        name = ns + "." + name;
                    }
                }
            }

            return null == name ? null : string.Intern(name);
        }

        public static string[] ConvertName(this SyntaxNode node)
        {
            var names = new List<string>();
            if (node is IdentifierNameSyntax)
            {
                var name = ((IdentifierNameSyntax)node).Identifier.Text;
                names.Add(string.Intern(name));
            }
            else if (node is GenericNameSyntax)
            {
                var genericname = (GenericNameSyntax)node;
                var name = genericname.Identifier.Text;

                name += "`" + genericname.TypeArgumentList.Arguments.Count;
                names.Add(string.Intern(name));
            }
            else if (node is QualifiedNameSyntax)
            {
                var qname = (QualifiedNameSyntax)node;
                names.AddRange(ConvertName(qname.Left));
                names.AddRange(ConvertName(qname.Right));
            }
            else if (node is PredefinedTypeSyntax)
            {
                names.Add(node.ToString());
            }
            else if (node is AttributeSyntax)
            {
                var a = (AttributeSyntax)node;
                names.Add(a.Name.ToString() + "Attribute");
            }
            else if (node is MemberAccessExpressionSyntax)
            {
                var m = (MemberAccessExpressionSyntax)node;
                names.AddRange(m.Expression.ConvertName());
                names.AddRange(m.Name.ConvertName());
            }
            else
            {
                Log.Warning("don't know how to translate name [" + node.Kind() + "]");
                return null;
            }

            return names.ToArray();
        }

        public static string GetFullName(this ITypeSymbol symbol)
        {
            var name = symbol.Name;
            INamespaceSymbol ns = symbol.ContainingNamespace;

            while(ns != null && !ns.IsGlobalNamespace)
            {
                name = ns.Name + "." + name;
                ns = ns.ContainingNamespace;
            }

            return name;
        }

        public static bool TryGetTree(this ISourceInfo source, out SyntaxTree result)
        {
            if (source is SourceInfo)
            {
                result = ((SourceInfo)source).Tree;
                return null != result;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public static ITypeSymbol GetTypeSymbol(this ISymbol s)
        {
            ITypeSymbol result = null;
            if (s is ITypeSymbol)
            {
                result = (ITypeSymbol)s;
            }
            else if (s is IPropertySymbol)
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
            else if(null == s)
            {
                // ok
            }
            else
            {
                Log.Warning("not handled left kind {0} of [{1}]", s.Kind, s);
            }

            return result;
        }
    }
}
