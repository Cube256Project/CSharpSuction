using Common;
using Common.Tokenization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpSuction;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpSuction.Generation
{
    /// <summary>
    /// Emit type script code corresponding to the syntax trees given.
    /// </summary>
    public class EmitTypeScript : Emit
    {
        #region Private

        private Dictionary<ITypeInfo, string> _factory = new Dictionary<ITypeInfo, string>();
        private HashSet<string> _backingfields = new HashSet<string>();
        private bool _hasuper = false;
        private bool _hasctor;
        private SyntaxNode TypeNode;
        private int _varseed;
        private TokenWriter _currentwriter = null;
        private Stack<ScriptStateModifier> _stack = new Stack<ScriptStateModifier>();

        #endregion

        #region Properties

        /// <summary>
        /// The current output writer.
        /// </summary>
        private TokenWriter Writer
        {
            get { return _currentwriter; }
        }

        private ITypeInfo Current { get; set; }

        private SemanticModel Model { get; set; }

        public bool CompileTypeScript = false;

        #endregion

        protected override bool Generate()
        {
            try
            {
                Push(new TokenWriter());

                WriteLine("// generated with version " + GetType().Assembly.GetName().Version);
                WriteLine();

                var types = new DependencyOrdering().Order(Suction, Suction.Types).ToList();

                foreach (var type in types)
                {
                    EmitType(type);
                }

                EmitFactory();

                Directory.CreateDirectory(OutputDirectory);

                var filename = "generated.ts";
                var path = Path.Combine(OutputDirectory, filename);
                File.WriteAllText(path, Writer.Text);

                Log.Information("generated type script {0}.", path.Quote());

                if (CompileTypeScript)
                {
                    var appdir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                    Log.Debug("converting typescript ...");

                    var prun = new ProcessRunner();
                    prun.WorkingDirectory = OutputDirectory;
                    prun.FileName = Path.Combine(appdir, @"npm\tsc.cmd");
                    prun.Arguments = "-t ES5 " + filename;
                    prun.OnOutput += (sender, e) => Log.Debug("> {0}", e.Data);
                    prun.Start();
                    var exitcode = prun.Wait();

                    Log.Information("converted to jscript {0}.", exitcode);
                }
            }
            finally
            {
                Pop();
            }

            return true;
        }

        #region Utility

        private void EnterBlock()
        {
            Writer.WriteLine("{");
            Writer.Indent();
        }

        private void LeaveBlock()
        {
            Writer.UnIndent();
            Writer.WriteLine("}");
        }

        private void Write(string s) { Writer.Write(s); }

        private void WriteLine(string s = null) { Writer.WriteLine(s); }

        private void WriteComment(string s)
        {
            WriteLine();
            WriteLine("/* " + s + " */");
        }

        #endregion

        #region Object Factory Generation

        /// <summary>
        /// Emits the [ObjectFactory.CreateInstance] method.
        /// </summary>
        private void EmitFactory()
        {
            WriteLine("class ObjectFactory");
            EnterBlock();
            WriteLine("public static CreateInstance(type: string): any");
            EnterBlock();
            WriteLine("switch(type)");
            EnterBlock();
            foreach (var pair in _factory)
            {
                var info = pair.Key;

                // Writer.WriteLine("case " + pair.Key.Quote() + ": return new " + pair.Value + "()");
                WriteLine("case " + info.QualifiedName.CQuote() + ":");
                var node = info.Nodes().FirstOrDefault();
                if (null != node)
                {
                    WriteLine("case " + ConvertType(node).CQuote() + ":");
                }

                foreach (var e in info.Nodes())
                {
                    foreach (var list in e.ChildNodes().OfType<AttributeListSyntax>())
                    {
                        foreach (var a in list.Attributes)
                        {
                            var aname = a.Name.ToString();
                            if (aname == "SerializationSyntax")
                            {
                                var farg = a.ArgumentList.Arguments.FirstOrDefault();
                                foreach (var q in ConvertSerializationSyntax(farg))
                                {
                                    WriteLine("case " + q + ":");
                                }
                            }
                        }
                    }
                }

                Writer.Indent();
                WriteLine("return new " + pair.Value + "();");
                WriteLine();
                Writer.UnIndent();

            }
            Writer.WriteLine("default: throw new Error('unknown type ' + type + '.')");
            LeaveBlock();
            LeaveBlock();
            LeaveBlock();
        }

        private IEnumerable<string> ConvertSerializationSyntax(AttributeArgumentSyntax farg)
        {
            var expr = farg.Expression;
            Model = Suction.Compilation.GetSemanticModel(farg.SyntaxTree, false);
            IFieldSymbol field;
            if (TryGetSymbol(expr, out field))
            {
                if (field.IsStatic && field.IsConst && field.HasConstantValue)
                {
                    yield return ("#" + (int)field.ConstantValue).CQuote();
                }
                else
                {
                    throw new Exception("unable to convert serialization attribute.");
                }
            }
            else if(expr is LiteralExpressionSyntax)
            {
                yield return expr.ToString();
            }
            else
            {
                throw new Exception("failed to obtain symbol for attribute argument " + farg.ToString().Quote() + ".");
            }

            yield break;
        }

        #endregion

        #region Modifier Stack

        internal void Push(ScriptStateModifier mod)
        {
            _stack.Push(mod);

            if(mod is WriterRedirectModifier)
            {
                _currentwriter = ((WriterRedirectModifier)mod).Writer;
            }
        }

        private void Push(TokenWriter writer)
        {
            Push(new WriterRedirectModifier(writer));
        }

        internal void Pop(int count = 1)
        {
            while (count-- > 0)
            {
                var mod = _stack.Peek();

                _stack.Pop();

                if (mod is WriterRedirectModifier)
                {
                    _currentwriter = _stack.OfType<WriterRedirectModifier>().Select(e => e.Writer).FirstOrDefault();
                }
            }
        }

        #endregion

        #region Declarations

        private void EmitType(ITypeInfo type)
        {
            Current = type;

            // TODO: make modifier?
            _backingfields.Clear();
            _hasuper = false;

            var first = true;
            foreach (var node in type.Nodes().Distinct())
            {
                // need to model to decide
                Model = Suction.Compilation.GetSemanticModel(node.SyntaxTree, false);

                // if node is ignored
                if (IsIgnored(node))
                {
                    return;
                }

                if (first)
                {
                    // emit declaration header
                    if (EmitTypeHeader(node))
                    {
                        first = false;
                    }
                    else
                    {
                        break;
                    }
                }

            }

            if (!first)
            {
                EmitClassFooter();
            }
        }

        private bool EmitTypeHeader(SyntaxNode node)
        {
            TypeNode = node;
            if (node is ClassDeclarationSyntax)
            {
                EmitClassHeader((ClassDeclarationSyntax)node);
                return true;
            }
            else if (node is InterfaceDeclarationSyntax)
            {
                EmitInterfaceHeader((InterfaceDeclarationSyntax)node);
                return true;
            }
            else if(node is EnumDeclarationSyntax)
            {
                EmitEnumDeclaration((EnumDeclarationSyntax)node);
                return false;
            }
            else
            {
                Log.Warning("type syntax [" + node.GetType().Name + "] ignored.");
                return false;
            }
        }

        private void EmitEnumDeclaration(EnumDeclarationSyntax node)
        {
            var name = ConvertType(node);
            WriteComment(name + " from " + node.SyntaxTree.FilePath.Quote());
            WriteTypePreabmle();
            WriteLine("enum " + name);
            EnterBlock();

            foreach (var e in node.Members)
            {
                EmitCode(e);
                Writer.WriteLine(",");
            }

            LeaveBlock();
            WriteLine();
        }

        private void EmitInterfaceHeader(InterfaceDeclarationSyntax node)
        {
            var name = node.Identifier.Text;

            WriteComment(name + " from " + node.SyntaxTree.FilePath.Quote());
            WriteTypePreabmle();
            Writer.Write("interface " + name);

            EmitBaseList(node.BaseList, true);

            Writer.Indent();
            Writer.WriteLine("{");

            EmitTypeMembers(node.Members);
        }

        private void EmitClassHeader(ClassDeclarationSyntax node)
        {
            var name = node.Identifier.Text;

            try
            {
                WriteComment(name + " from " + node.SyntaxTree.FilePath.Quote());
                EmitTypeModifiers(node.Modifiers);
                WriteLine("class " + name);

                if (!node.Modifiers.Any(m => m.Text == "abstract"))
                {
                    _factory.Add(Current, name);
                }

                _hasuper = EmitBaseList(node.BaseList, false);
                
                // parameterless ctor?
                _hasctor = node.ChildNodes().OfType<ConstructorDeclarationSyntax>().Where(c => !c.ParameterList.Parameters.Any()).Any();

                EnterBlock();

                EmitTypeInfo(node, name);

                // Writer.WriteLine("public $type = " + name.CQuote());

                if (!_hasctor)
                {
                    // default constructor
                    WriteComment("automatic constructor");
                    WriteLine("constructor()");
                    EnterBlock();
                    if (_hasuper) WriteLine(" super();");
                    LeaveBlock();
                }

                EmitTypeMembers(node.Members);
            }
            catch (Exception ex)
            {
                throw new Exception("error in class [" + name + "]: " + ex.Message);
            }
        }

        private void EmitTypeModifiers(IEnumerable<SyntaxToken> modifiers)
        {
            foreach (var modifier in modifiers.Select(m => m.Text))
            {
                switch (modifier)
                {
                    case "abstract":
                        Write(modifier);
                        Writer.WriteSpace();
                        break;
                }
            }
        }

        private void WriteTypePreabmle()
        {
        }

        private void EmitTypeInfo(SyntaxNode node, string name)
        {
            // need type symbol for base type
            var symbol = Model.GetDeclaredSymbol(node) as ITypeSymbol;
            if (null == symbol)
            {
                throw new Exception("unable to get symbol for type " + name.Quote() + ".");
            }

            // construct basetype reference
            string basetype = "null";
            if (IsTypeIncluded(symbol.BaseType))
            {
                var s = ConvertType(symbol.BaseType);
                if (s != "any")
                {
                    basetype = s + ".$gt";
                }
            }

            // traditional, json compatible implementation
            WriteLine("public $type = " + name.CQuote() + ";");

            // extended type information (.NET like)
            WriteLine("public static $gt = new Type(" + name.CQuote() + ", " + basetype + ");");
            WriteLine("public GetType(): Type { return " + name + ".$gt; }");
        }

        private void EmitMemberModifiers(IEnumerable<SyntaxToken> modifiers)
        {
            foreach (var modifier in modifiers.Select(m => m.Text))
            {
                switch (modifier)
                {
                    case "abstract":
                    case "public":
                    case "private":
                    case "static":
                        Write(modifier);
                        Writer.WriteSpace();
                        break;
                }
            }
        }

        private bool EmitBaseList(BaseListSyntax baselist, bool isinterface)
        {
            var result = false;
            if (null != baselist)
            {
                var firstimplements = true;
                foreach (var e in baselist.Types)
                {
                    var info = Model.GetSymbolInfo(e.Type);
                    var typeinfo = info.Symbol as ITypeSymbol;
                    if (null != typeinfo && IsTypeIncluded(typeinfo))
                    {
                        if (typeinfo.TypeKind == TypeKind.Class)
                        {
                            Writer.Write(" extends " + typeinfo.Name);
                            result = true;
                        }
                        else if (typeinfo.TypeKind == TypeKind.Interface)
                        {
                            if (IsIgnored(typeinfo))
                            {
                                continue;
                            }

                            if (firstimplements)
                            {
                                Writer.Write(isinterface ? " extends " : " implements ");
                                firstimplements = false;
                            }
                            else
                            {
                                Writer.Write(", ");
                            }

                            Writer.WriteLine(typeinfo.Name);
                        }
                    }
                }
            }

            return result;
        }

        private void EmitTypeMembers(IEnumerable<MemberDeclarationSyntax> members)
        {
            foreach (var member in members)
            {
                if (IsIgnored(member))
                {
                    // ignore
                    // Log.Debug("member [{0}] [{1}] was ignored.", member.GetDeclaration(), member.GetType().Name);
                }
                else if (member is FieldDeclarationSyntax)
                {
                    EmitFieldDeclaration((FieldDeclarationSyntax)member);
                }
                else if (member is PropertyDeclarationSyntax)
                {
                    EmitPropertyDeclaration((PropertyDeclarationSyntax)member);
                }
                else if (member is MethodDeclarationSyntax)
                {
                    EmitMethodDeclaration((MethodDeclarationSyntax)member);
                }
                else if (member is ConstructorDeclarationSyntax)
                {
                    EmitConstructorDeclaration((ConstructorDeclarationSyntax)member);
                }
                else if (member is EventFieldDeclarationSyntax)
                {
                    EmitEventFieldDeclaration((EventFieldDeclarationSyntax)member);
                }
                else
                {
                    Log.Warning("member syntax [" + member.GetType().Name + "] ignored.");
                }
            }
        }

        private void EmitEventFieldDeclaration(EventFieldDeclarationSyntax member)
        {
            foreach (var v in member.Declaration.Variables)
            {
                var name = v.Identifier.Text;
                var back = MakeBackingFieldName(name);

                WriteComment("member " + name + " [" + member.GetType().Name + "]");

                if (TypeNode is ClassDeclarationSyntax)
                {
                    // event backing field, a Delegate.
                    WriteLine("private " + back + ": Delegate = null;");
                    back = "this." + back;

                    // accessors
                    WriteLine("public add" + name + "(a: Function) { " + back + " = Delegate.Combine(" + back + ", a); }");
                    WriteLine("public remove" + name + "(a: Function) { " + back + " = Delegate.Remove(" + back + ", a); }");
                }
                else
                {
                    // interface; accessor declarations only
                    WriteLine("add" + name + "(a: Function);");
                    WriteLine("remove" + name + "(a: Function);");
                }
            }
        }

        private string DeriveNonDefaultConstructorName(ConstructorDeclarationSyntax member, out ClassDeclarationSyntax type)
        {
            type = (ClassDeclarationSyntax)member.Parent;
            var index = type.Members.IndexOf(member);

            return "__ctor" + index;
        }

        private void EmitConstructorDeclaration(ConstructorDeclarationSyntax member)
        {
            if (IsIgnored(member)) return;

            WriteComment("constructor: " + member.ParameterList.ToString());

            var isdefault = !member.ParameterList.Parameters.Any();

            string dctor = null;
            string tmp = null;

            if (!isdefault)
            {
                // not the default constructor, change to static
                //Write("static constructor" + ++_ctorseq);

                ClassDeclarationSyntax type;
                Write("static " + DeriveNonDefaultConstructorName(member, out type));

                dctor = ConvertType(type);
            }
            else
            {
                Writer.Write("constructor");
            }

            EmitCode(member.ParameterList);
            WriteLine();

            // customize block
            EnterBlock();

            if (null == dctor)
            {
                if (_hasuper) WriteLine(" super();");
            }
            else
            {
                tmp = CreateTemporaryVariable();
                WriteLine("let " + tmp + " = new " + dctor + "();");

                Push(new ThisRedirectModifier(tmp));
            }

            EmitOuterThisIfRequired(member.Body);

            foreach (var s in member.Body.Statements)
            {
                EmitCode(s);
            }

            if (null != dctor)
            {
                Writer.WriteLine("return " + tmp);
                Pop();
            }

            LeaveBlock();

        }

        private void EmitMethodDeclaration(MethodDeclarationSyntax member)
        {
            var name = member.Identifier.Text;

            if (IsIgnored(member))
            {
                return;
            }

            // omit extension methods
            if (member.ParameterList.Parameters.Where(p => p.Modifiers.Any(m => m.IsKind(SyntaxKind.ThisKeyword))).Any())
            {
                // Log.Warning("method {0}.{1} is an extension method, ignored.", Current.QualifiedName, name);
                return;
            }

            if(ContainsYieldStatement(member.Body))
            {
                Log.Warning("method {0}.{1} contains a yield statement, ignored.", Current.QualifiedName, name);
                return;
            }

            // method header
            WriteComment("member " + name + " method");
            EmitMemberModifiers(member.Modifiers);
            Writer.Write(name);

            EmitCode(member.ParameterList);

            if (null != member.ReturnType)
            {
                Write(": ");
                Write(ConvertType(member.ReturnType));
            }

            if (null == member.Body)
            {
                WriteLine(";");
            }
            else
            {
                // method body
                WriteLine();
                EmitMethodBody(member.Body);
            }
        }

        private void EmitMethodBody(BlockSyntax body)
        {
            EnterBlock();

            EmitOuterThisIfRequired(body);

            foreach (var stmt in body.Statements)
            {
                EmitCode(stmt);
            }

            LeaveBlock();
        }

        private void EmitOuterThisIfRequired(SyntaxNode node)
        {
            var required = false;

            if(node.DescendantNodes().OfType<LambdaExpressionSyntax>().Any())
            {
                required = true;
            }
            else if(node.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Where(e => null != e.Initializer).Any())
            {
                required = true;
            }

            // contains lambda expressions?
            if (required)
            {
                WriteLine("let $otis = this;");
            }
        }

        private void EmitPropertyDeclaration(PropertyDeclarationSyntax decl)
        {
            var name = decl.Identifier.Text;

            WriteComment("member " + name + " property");

            var type = ConvertType(decl.Type);

            if (IsInterface())
            {
                Writer.WriteLine(name + ": " + type + ";");
                return;
            }

            foreach (var accessor in decl.AccessorList.Accessors)
            {
                var automatic = null == accessor.Body;
                if (automatic)
                {
                    EmitBackingField(name, type);
                }

                if (accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
                {
                    EmitMemberModifiers(decl.Modifiers);
                    Writer.Write("get " + name + "(): " + type);
                    if (!automatic)
                    {
                        Writer.WriteLine();
                        EmitCode(accessor.Body);
                    }
                    else
                    {
                        Writer.WriteLine();
                        EnterBlock();
                        Writer.WriteLine("return this." + MakeBackingFieldName(name) + ";");
                        LeaveBlock();
                    }
                }
                else if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
                {
                    EmitMemberModifiers(decl.Modifiers);
                    Writer.Write("set " + name + "(a: " + type + ")");
                    if (!automatic)
                    {
                        Writer.WriteLine();
                        EmitCode(accessor.Body);
                    }
                    else 
                    {
                        Writer.WriteLine();
                        EnterBlock();
                        Writer.WriteLine("this." + MakeBackingFieldName(name) + " = a;");

                        if (IsAutoNotify(decl))
                        {
                            // OGQQIVRSJG
                            WriteLine("this.TriggerPropertyChanged(" + name.CQuote() + ");");
                        }

                        LeaveBlock();
                    }
                }
            }
        }

        private void EmitFieldDeclaration(FieldDeclarationSyntax decl)
        {
            foreach (var v in decl.Declaration.Variables)
            {
                EmitMemberModifiers(decl.Modifiers);
                Write(v.Identifier.Text);
                Write(": ");
                Write(ConvertType(decl.Declaration.Type));
                if (null != v.Initializer)
                {
                    Write(" = ");
                    EmitCode(v.Initializer.Value);
                }
                WriteLine(";");
            }
        }

        #endregion

        #region Backing Fields

        private void EmitBackingField(string name, string type)
        {
            name = MakeBackingFieldName(name);

            if (_backingfields.Add(name))
            {
                Writer.WriteLine("private " + name + ": " + type + ";");
            }
        }

        private string MakeBackingFieldName(string name)
        {
            name = "_" + name.ToLower();
            return name;
        }

        #endregion

        #region Statements

        private void EmitCode(SyntaxNode value)
        {
            if (value == null)
            {
                // no effect
            }
            else if (value is BlockSyntax)
            {
                EmitBlock((BlockSyntax)value);
            }
            else if (value is ReturnStatementSyntax)
            {
                EmitReturn((ReturnStatementSyntax)value);
            }
            else if (value is IfStatementSyntax)
            {
                EmitIfStatement((IfStatementSyntax)value);
            }
            else if (value is ElseClauseSyntax)
            {
                EmitElseCaluse((ElseClauseSyntax)value);
            }
            else if (value is ExpressionStatementSyntax)
            {
                EmitExpressionStatement((ExpressionStatementSyntax)value);
            }
            else if (value is ThrowStatementSyntax)
            {
                EmitThrowStatement((ThrowStatementSyntax)value);
            }
            else if (value is LocalDeclarationStatementSyntax)
            {
                EmitLocalDeclarationStatement((LocalDeclarationStatementSyntax)value);
            }
            else if (value is ForStatementSyntax)
            {
                EmitForStatementSyntax((ForStatementSyntax)value);
            }
            else if (value is ForEachStatementSyntax)
            {
                EmitForEachStatement((ForEachStatementSyntax)value);
            }
            else if (value is WhileStatementSyntax)
            {
                EmitWhileStatement((WhileStatementSyntax)value);
            }
            else if (value is BreakStatementSyntax)
            {
                WriteLine("break;");
            }
            else if (value is EnumMemberDeclarationSyntax)
            {
                EmitEnumMemberDeclaration((EnumMemberDeclarationSyntax)value);
            }
            else
            {
                Writer.Write(MakeCode(value));
            }
        }

        private void EmitEnumMemberDeclaration(EnumMemberDeclarationSyntax value)
        {
            Write(value.Identifier.Text);

            if (null != value.EqualsValue)
            {
                Write(" = ");
                EmitCode(value.EqualsValue.Value);
            }
        }

        private void EmitWhileStatement(WhileStatementSyntax value)
        {
            Write("while(");
            EmitCode(value.Condition);
            WriteLine(")");
            EmitCode(value.Statement);
        }

        private void EmitForEachStatement(ForEachStatementSyntax value)
        {
            var source = MakeCode(value.Expression);
            var name = value.Identifier.Text;
            //WriteLine("for(let " + value.Identifier.Text + " of " + MakeCode(value.Expression) + ")");
            EnterBlock();

            var it = CreateTemporaryVariable();
            WriteLine("let " + it + " = " + source + ".GetEnumerator()");
            WriteLine("while(" + it + ".MoveNext())");
            EnterBlock();
            WriteLine("let " + name + " = " + it + ".Current;");
            EmitCode(value.Statement);
            LeaveBlock(); // while
            LeaveBlock(); // wrap
        }

        private void EmitForStatementSyntax(ForStatementSyntax value)
        {
            Write("for(");
            //EmitCode(value.Declaration);

            if(null != value.Initializers)
            {
                Write(value.Initializers.Select(e => MakeCode(e)).ToSeparatorList());
            }

            Write("; ");

            if(null != value.Condition)
            {
                EmitCode(value.Condition);
            }

            Write("; ");

            if (null != value.Incrementors)
            {
                Write(value.Incrementors.Select(e => MakeCode(e)).ToSeparatorList());
            }

            WriteLine(")");

            EmitCode(value.Statement);
        }

        private void EmitElseCaluse(ElseClauseSyntax value)
        {
            Write(" else ");
            EmitCode(value.Statement);
        }

        private void EmitLocalDeclarationStatement(LocalDeclarationStatementSyntax value)
        {
            var decl = value.Declaration;
            foreach (var e in decl.Variables)
            {
                Write("let " + e.Identifier);
                if (null != e.Initializer)
                {
                    Write(" = ");
                    Write(MakeCode(e.Initializer.Value));
                }

                WriteLine(";");
            }
        }

        private void EmitThrowStatement(ThrowStatementSyntax value)
        {
            WriteLine("throw " + MakeCode(value.Expression) + ";");
        }

        private void EmitExpressionStatement(ExpressionStatementSyntax value)
        {
            EmitCode(value.Expression);
            WriteLine(";");
        }

        private void EmitIfStatement(IfStatementSyntax value)
        {
            Write("if(");
            EmitCode(value.Condition);
            Write(")");
            EmitCode(value.Statement);
            if(null != value.Else)
            {
                EmitCode(value.Else);
            }

            WriteLine();
        }

        private void EmitBlock(BlockSyntax value)
        {
            EnterBlock();
            foreach(var s in value.Statements)
            {
                EmitCode(s);
            }
            LeaveBlock();
        }

        private void EmitReturn(ReturnStatementSyntax value)
        {
            if (null != value.Expression)
            {
                WriteLine("return " + MakeCode(value.Expression) + ";");
            }
            else
            {
                WriteLine("return;");
            }
        }

        #endregion

        #region Expressions

        private string MakeCode(SyntaxNode value)
        {
            if (value is ObjectCreationExpressionSyntax)
            {
                return MakeObjectCreationExpression((ObjectCreationExpressionSyntax)value);
            }
            else if (value is IdentifierNameSyntax)
            {
                return MakeIdentifierName((IdentifierNameSyntax)value);
            }
            else if (value is ParameterListSyntax)
            {
                return MakeParameterList((ParameterListSyntax)value);
            }
            else if (value is ParameterSyntax)
            {
                return MakeParameter((ParameterSyntax)value);
            }
            else if (value is BinaryExpressionSyntax)
            {
                return MakeBinaryExpression((BinaryExpressionSyntax)value);
            }
            else if (value is LiteralExpressionSyntax)
            {
                return MakeLiteralExpression((LiteralExpressionSyntax)value);
            }
            else if (value is InvocationExpressionSyntax)
            {
                return MakeInvocationExpression((InvocationExpressionSyntax)value);
            }
            else if (value is ArgumentSyntax)
            {
                return MakeArgument((ArgumentSyntax)value);
            }
            else if (value is ThisExpressionSyntax)
            {
                return MakeThisExpression((ThisExpressionSyntax)value);
            }
            else if (value is BaseExpressionSyntax)
            {
                return MakeBaseExpression((BaseExpressionSyntax)value);
            }
            else if (value is MemberAccessExpressionSyntax)
            {
                return MakeMemberAccessExpression((MemberAccessExpressionSyntax)value);
            }
            else if (value is AssignmentExpressionSyntax)
            {
                return MakeAssignmentExpression((AssignmentExpressionSyntax)value);
            }
            else if (value is PredefinedTypeSyntax)
            {
                return TranslateTypeName(((PredefinedTypeSyntax)value).ToString());
            }
            else if (value is CastExpressionSyntax)
            {
                return MakeCastExpression((CastExpressionSyntax)value);
            }
            else if (value is ConditionalExpressionSyntax)
            {
                return MakeConditionalExpression((ConditionalExpressionSyntax)value);
            }
            else if (value is PrefixUnaryExpressionSyntax)
            {
                return MakePrefixUnaryExpression((PrefixUnaryExpressionSyntax)value);
            }
            else if (value is PostfixUnaryExpressionSyntax)
            {
                return MakePostfixUnaryExpression((PostfixUnaryExpressionSyntax)value);
            }
            else if (value is ElementAccessExpressionSyntax)
            {
                return MakeElementAccessExpression((ElementAccessExpressionSyntax)value);
            }
            else if (value is ArrayCreationExpressionSyntax)
            {
                return MakeArrayCreationExpression((ArrayCreationExpressionSyntax)value);
            }
            else if (value is TypeOfExpressionSyntax)
            {
                return MakeTypeOfExpression((TypeOfExpressionSyntax)value);
            }
            else if (value is ParenthesizedExpressionSyntax)
            {
                return MakeParenthesizedExpression((ParenthesizedExpressionSyntax)value);
            }
            else if (value is SimpleLambdaExpressionSyntax)
            {
                return MakeSimpleLambdaExpression((SimpleLambdaExpressionSyntax)value);
            }
            else if (value is ParenthesizedLambdaExpressionSyntax)
            {
                return MakeParenthesizedLambdaExpression((ParenthesizedLambdaExpressionSyntax)value);
            }
            else if (value is InitializerExpressionSyntax)
            {
                return MakeInitializerExpression((InitializerExpressionSyntax)value);
            }
            else
            {
                throw new NotImplementedException("[" + value.GetType().Name + "]: " + value);
            }
        }

        private string MakeInitializerExpression(InitializerExpressionSyntax value)
        {
            return value.Expressions.Select(e => MakeCode(e)).ToSeparatorList();
        }

        private void EmitLambdaBody(SyntaxNode body)
        {
            Push(new ThisRedirectModifier("$otis"));

            if (body is BlockSyntax)
            {
                EmitCode(body);
            }
            else
            {
                EnterBlock();
                EmitCode(body);
                LeaveBlock();
            }

            Pop();
            Pop();
        }

        private string MakeParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax value)
        {
            var result = new TokenWriter();
            Push(new WriterRedirectModifier(result));

            WriteLine("function(" + value.ParameterList.Parameters.Select(e => MakeCode(e)).ToSeparatorList() + ")");

            EmitLambdaBody(value.Body);

            return result.Text;
        }

        private string MakeSimpleLambdaExpression(SimpleLambdaExpressionSyntax value)
        {
            var result = new TokenWriter();
            Push(new WriterRedirectModifier(result));

            WriteLine("function(" + value.Parameter.Identifier.Text + ")");

            EmitLambdaBody(value.Body);

            return result.Text;
        }

        private string MakeParenthesizedExpression(ParenthesizedExpressionSyntax value)
        {
            return "(" + MakeCode(value.Expression) + ")";
        }

        private string MakeTypeOfExpression(TypeOfExpressionSyntax value)
        {
            var typename = ConvertType(value.Type);
            return "TypeOperations.GetTypeOf(" + typename + ")";
        }

        private string MakeArrayCreationExpression(ArrayCreationExpressionSyntax value)
        {
            // TODO: this is incomplete
            Log.Warning("TODO: [MakeArrayCreationExpression] initializer!");
            return "[]";
        }

        private string MakeElementAccessExpression(ElementAccessExpressionSyntax value)
        {
            return MakeCode(value.Expression) + 
                "[" +
                value.ArgumentList.Arguments.Select(e => MakeCode(e)).ToSeparatorList() +
                "]";
        }

        private string MakePostfixUnaryExpression(PostfixUnaryExpressionSyntax value)
        {
            return MakeCode(value.Operand) + value.OperatorToken;
        }

        private string MakePrefixUnaryExpression(PrefixUnaryExpressionSyntax value)
        {
            return value.OperatorToken + MakeCode(value.Operand);
        }

        private string MakeBinaryExpression(BinaryExpressionSyntax value)
        {
            var op = value.OperatorToken.Text;
            if (op == "is")
            {
                var info = Model.GetSymbolInfo(value.Right);
                if (info.Symbol is ITypeSymbol)
                {
                    var typename = ConvertType((ITypeSymbol)info.Symbol);

                    return "TypeOperations.IsOfType(" + MakeCode(value.Left) + ", " + typename + ")";
                }
                else
                {
                    throw new Exception("expected type symbol for " + value.ToString().Quote() + ".");
                }
            }
            else if (op == "??")
            {
                // TODO: improper
                return MakeCode(value.Left) + " ? " + MakeCode(value.Left) + " : " + MakeCode(value.Right);
            }
            else
            {
                return MakeCode(value.Left) + " " + value.OperatorToken.Text + " " + MakeCode(value.Right);
            }
        }

        private string MakeConditionalExpression(ConditionalExpressionSyntax value)
        {
            return MakeCode(value.Condition) + " ? " + MakeCode(value.WhenTrue) + " : " + MakeCode(value.WhenFalse);
        }

        private string MakeCastExpression(CastExpressionSyntax value)
        {
            return MakeCode(value.Expression) + " as " + ConvertType(value.Type);
        }

        private string MakeBaseExpression(BaseExpressionSyntax value)
        {
            return "super";
        }

        private string MakeAssignmentExpression(AssignmentExpressionSyntax value)
        {
            string left, right;
            var op = value.OperatorToken.Text;
            if (op == "+=")
            {
                ISymbol symbol;
                if (TryGetSymbol(value.Left, out symbol))
                {
                    if (symbol.Kind == SymbolKind.Event)
                    {
                        Push(new EventAssignmentModifier());
                        left = MakeCode(value.Left);
                        Pop();
                        var result = left + "(" + MakeCode(value.Right) + ")";
                        return result;
                    }
                }
            }

            // inside an initializer?
            var init = _stack.OfType<InitializerModifier>().FirstOrDefault();
            if (null != init)
            {
                using (new StateSection(this, new ThisRedirectModifier(init.AlternativeLeft)))
                {
                    left = MakeCode(value.Left);
                }

                using (new StateSection(this, new ThisRedirectModifier(init.ThisName)))
                {
                    right = MakeCode(value.Right);
                }
            }
            else
            {
                left = MakeCode(value.Left);
                right = MakeCode(value.Right);
            }

            return left + " " + value.OperatorToken + " " + right;
        }

        private string MakeMemberAccessExpression(MemberAccessExpressionSyntax value)
        {
            if (value.Name is GenericNameSyntax)
            {
            }

            var left = Model.GetSymbolInfo(value.Expression);
            var name = value.Name.ToString();

            ISymbol symbol;
            if (TryGetSymbol(value, out symbol))
            {
                if (symbol.Kind == SymbolKind.Event)
                {
                    if (_stack.OfType<EventAssignmentModifier>().Any())
                    {
                        name = "add" + name;
                    }
                }
            }

            var expressiontype = left.Symbol.GetTypeSymbol();
            if (null != expressiontype)
            {
                // oportunity to modify the right side
                name = TranslateMemberAccess(expressiontype, name);
            }

            /*if (null != left.Symbol && null != left.Symbol.ContainingType)
            {
                name = TranslateMemberAccess(left.Symbol.ContainingType, name);
            }*/ 

            return MakeCode(value.Expression) + "." + name;
        }

        private string MakeThisExpression(ThisExpressionSyntax value)
        {
            return GetCurrentThis();
        }

        private string MakeArgument(ArgumentSyntax value)
        {
            if (!value.RefOrOutKeyword.IsKind(SyntaxKind.None))
            {
                throw new Exception("out/ref not supported.");
            }

            return MakeCode(value.Expression);
        }

        private string MakeInvocationExpression(InvocationExpressionSyntax value)
        {
            var info = Model.GetSymbolInfo(value.Expression);

            var expression = MakeCode(value.Expression);
            var arguments = value.ArgumentList.Arguments.Select(e => MakeCode(e)).ToList();

            if (null != info.Symbol)
            {
                var s = info.Symbol;

                if (s.Kind == SymbolKind.Event)
                {
                    arguments.Insert(0, expression);
                    expression = "Delegate.Invoke";
                }
            }

            return expression + "(" + arguments.ToSeparatorList() + ")";
        }

        private string MakeLiteralExpression(LiteralExpressionSyntax value)
        {
            var s = value.ToString();
            if (s.StartsWith("@"))
            {
                //throw new Exception("string '@' not supported.");
                return s.Substring(1).Replace("\\", "\\\\");
            }
            else
            {
                return s;
            }
        }

        private string MakeParameter(ParameterSyntax value)
        {
            var typename = null == value.Type ? "any" : ConvertType(value.Type);
            return value.Identifier.Text + ": " + typename;
        }

        private string MakeParameterList(ParameterListSyntax value)
        {
            return "(" + value.Parameters.Select(e => MakeCode(e)).ToSeparatorList() + ")";
        }

        private string MakeIdentifierName(IdentifierNameSyntax value)
        {
            var info = Model.GetSymbolInfo(value);
            ISymbol symbol;
            if(null == info.Symbol)
            {
                if (info.CandidateSymbols.Count() > 0)
                {
                    symbol = info.CandidateSymbols.First();
                }
                else
                {
                    throw new Exception("symbol " + value + " not found.");
                }
            }
            else
            {
                symbol = info.Symbol;
            }

            var name = symbol.Name;

            string lref;
            if (symbol.IsStatic)
            {
                lref = ConvertType(TypeNode);
            }
            else
            {
                lref = GetCurrentThis();
            }

            switch(symbol.Kind)
            {
                case SymbolKind.Field:
                case SymbolKind.Property:
                case SymbolKind.Method:
                    name = lref + "." + name;
                    break;

                case SymbolKind.Event:
                    if (_stack.OfType<EventAssignmentModifier>().Any())
                    {
                        // right side of assignment?
                        name = lref + ".add" + name;
                    }
                    else
                    {
                        // inside owner class?
                        name = lref + "." + MakeBackingFieldName(name);
                    }
                    break;
            }

            return name;
        }

        private string MakeObjectCreationExpression(ObjectCreationExpressionSyntax value)
        {
            var info = Model.GetSymbolInfo(value);
            string altname = null;
            if (info.Symbol is IMethodSymbol)
            {
                var symbol = (IMethodSymbol)info.Symbol;
                if (symbol.Kind == SymbolKind.Method && symbol.MethodKind == MethodKind.Constructor &&
                    null != value.ArgumentList &&
                    value.ArgumentList.Arguments.Any())
                {
                    // non default constructor call -> translate
                    var drefs = symbol.DeclaringSyntaxReferences;
                    if (drefs.Length > 0)
                    {
                        var syntax = (ConstructorDeclarationSyntax)drefs[0].GetSyntax();

                        ClassDeclarationSyntax ctype;
                        var ctor = DeriveNonDefaultConstructorName(syntax, out ctype);

                        altname = ConvertType(ctype) + "." + ctor;
                    }
                }
            }

            var type = TranslateClass(ConvertType(value.Type));

            var result = new TokenWriter();
            string tmp = null;

            if (null != value.Initializer)
            {
                tmp = CreateTemporaryVariable();
                result.Write("(function() { let " + tmp + " = ");
            }

            if (null == altname)
            {
                result.Write("new " + type);
            }
            else
            {
                result.Write(altname);
            }

            result.Write("(");
            if (null != value.ArgumentList)
            {
                result.Write(value.ArgumentList.Arguments.Select(e => MakeCode(e)).ToSeparatorList());
            }

            result.Write(")");

            if (null != value.Initializer)
            {
                result.Write("; ");

                Push(new InitializerModifier(tmp, "$otis"));
                result.Write(MakeCode(value.Initializer));
                result.Write("; ");
                Pop();

                result.Write("return " + tmp + ";");
                result.Write(" })()");
            }

            return result.Text;
        }

        #endregion

        #region Filtering

        private bool IsTypeIncluded(ITypeSymbol type)
        {
            //Log.Debug("type {0} :: {1}?", type.ContainingNamespace, type.Name);
            return true;
        }

        private bool IsIgnored(SyntaxNode node)
        {
            foreach (var e in node.ChildNodes().OfType<AttributeListSyntax>())
            {
                foreach (var a in e.Attributes)
                {
                    if (a.Name.ToString().Equals("GeneratorIgnore"))
                    {
                        return true;
                    }
                }
            }

            if (node is ClassDeclarationSyntax)
            {
                // filter attribute classes
                if (IsAttributeClass((ClassDeclarationSyntax)node))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsIgnored(ITypeSymbol typeinfo)
        {
            return false;
        }

        private bool IsAttributeClass(ClassDeclarationSyntax decl)
        {
            if(decl.Identifier.Text.EndsWith("Attribute"))
            {
                return true;
            }

            return false;
        }

        private bool IsInterface()
        {
            return TypeNode is InterfaceDeclarationSyntax;
        }

        private bool IsAutoNotify(PropertyDeclarationSyntax node)
        {
            foreach (var e in node.ChildNodes().OfType<AttributeListSyntax>())
            {
                foreach (var a in e.Attributes)
                {
                    if (a.Name.ToString().Equals("NotifyProperty"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region Type Translation

        /// <summary>
        /// Converts a syntax node into a type name.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private string ConvertType(SyntaxNode type)
        {
            if (type is EnumDeclarationSyntax)
            {
                var decl = (EnumDeclarationSyntax)type;
                return decl.Identifier.Text;
            }
            else if (type is ClassDeclarationSyntax)
            {
                return TranslateTypeName(((ClassDeclarationSyntax)type).Identifier.Text);
            }
            else if (type is InterfaceDeclarationSyntax)
            {
                return TranslateTypeName(((InterfaceDeclarationSyntax)type).Identifier.Text);
            }
            else if (type is EnumDeclarationSyntax)
            {
                return TranslateTypeName(((EnumDeclarationSyntax)type).Identifier.Text);
            }
            else
            {
                var info = Model.GetSymbolInfo(type);
                ITypeSymbol symbol;
                if (null != (symbol = info.Symbol as ITypeSymbol))
                {
                    return ConvertType(symbol, type);
                }
                else
                {
                    Log.Warning("no symbol for type declaration {0}.", type.ToString().Quote());
                    return "any";
                }
            }
        }

        private string ConvertType(ITypeSymbol symbol, SyntaxNode type = null)
        {
            string name;
            var result = new TokenWriter();

            if (symbol is IArrayTypeSymbol)
            {
                name = ConvertType(((IArrayTypeSymbol)symbol).ElementType) + "[]";
            }
            else
            {
                name = TranslateTypeName(symbol);
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new Exception("missing type name");
            }

            // TODO: beautify
            if (name == "Action")
            {
                return "Function";
            }

            // main name
            result.Write(name);

            // type parameters
            if (type is GenericNameSyntax)
            {
                var g = (GenericNameSyntax)type;
                result.Write("<" +
                    g.TypeArgumentList.Arguments.Select(e => ConvertType(e)).ToSeparatorList() +
                    ">");
            }

            return result.Text;
        }

        private string TranslateTypeName(ISymbol typesymbol)
        {
            return TranslateTypeName(typesymbol.Name);
        }

        private string TranslateTypeName(string name)
        { 
            switch(name)
            {
                case "Object":
                    return "any";

                case "Void":
                    return "void";

                case "String":
                    return "string";

                case "Boolean":
                    return "boolean";

                case "Int32":
                case "Int64":
                    return "number";

                case "Byte":
                case "Double":
                    return "number";

                default:
                    return name;
            }
        }

        private string TranslateClass(string type)
        {
            if (type == "Regex")
            {
                type = "RegExp";
            }

            return type;
        }

        #endregion

        #region Symbols

        private bool TryGetSymbol<T>(SyntaxNode node, out T result) where T : ISymbol
        {
            var info = Model.GetSymbolInfo(node);
            if (info.Symbol is T)
            {
                result = (T)info.Symbol;
                return true;
            }
            else
            {
                result = default(T);
                return false;
            }
        }

        #endregion

        private string GetCurrentThis()
        {
            var top = _stack.OfType<ThisRedirectModifier>().FirstOrDefault();

            return null == top ? "this" : top.ThisName;
        }

        private bool ContainsYieldStatement(SyntaxNode node)
        {
            return null == node ? false : node.DescendantNodes().Any(n => n is YieldStatementSyntax);
        }

        private void EmitClassFooter()
        {
            Writer.UnIndent();
            Writer.WriteLine("}");
            Writer.WriteLine();
        }

        private string TranslateMemberAccess(ITypeSymbol type, string member)
        {
            if (type.Name == "Regex")
            {
                if (member == "IsMatch")
                {
                    return "test";
                }
            }
            else if (0 == string.Compare(type.Name, "string", true))
            {
                if (member == "Split")
                {
                    return "split";
                }
                else if (member == "Length")
                {
                    return "length";
                }
            }
            else if (type is IArrayTypeSymbol)
            {
                if (member == "Length")
                {
                    return "length";
                }
            }

            return member;
        }

        private string CreateTemporaryVariable()
        {
            return "_" + ++_varseed;
        }
    }
}
