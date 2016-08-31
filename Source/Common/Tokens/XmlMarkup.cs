using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Tokens
{
    /// <summary>
    /// Basic composite class for XML tokens.
    /// </summary>
    public static class XmlMarkupParser
    {
        #region Private

        private enum State { initial, first, final, error, end, attrname, endelement, close, equalsign, attrvalue, notation, notiationcontent };

        private static bool IsName(Token token)
        {
            return token is XmlSimpleName;
        }

        #endregion

        public static XmlNode Combine(IEnumerable<Token> tokens)
        {
            var state = State.initial;
            var it = tokens.GetEnumerator();
            XmlNode result = null;
            Token attributename = null;
            Token attributevalue = null;

            while (state != State.final)
            {
                if (!it.MoveNext()) throw new Exception("xml error");
                var token = it.Current;

                if (token is Whitespace) continue;

                switch (state)
                {
                    case State.initial:
                        if (token is AngleBracketLeft)
                        {
                            state = State.first;
                        }
                        else
                        {
                            throw new Exception("expected '<'.");
                        }
                        break;

                    case State.first:
                        if (token is ForwardSlash)
                        {
                            state = State.endelement;
                        }
                        else if (IsName(token))
                        {
                            result = new XmlElement(token);
                            state = State.attrname;
                        }
                        else if(token is ExclamationMark)
                        {
                            state = State.notation;
                        }
                        else
                        {
                            throw new Exception("unexpected '" + token + "'.");
                        }
                        break;

                    case State.notation:
                        if (token is Identifier)
                        {
                            result = new XmlNotation(token);
                            state = State.notiationcontent;
                        }
                        else
                        {
                            throw new Exception("expected identifier.");
                        }
                        break;

                    case State.notiationcontent:
                        if(token is AngleBracketRight)
                        {
                            state = State.final;
                        }
                        break;

                    case State.attrname:
                        if(token is AngleBracketRight)
                        {
                            state = State.final;
                        }
                        else if (IsName(token))
                        {
                            attributename = token;
                            state = State.equalsign;
                        }
                        else if(token is ForwardSlash)
                        {
                            result.SetEmptyElement();
                            state = State.close;
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                        break;

                    case State.equalsign:
                        if (token is EqualSign)
                        {
                            state = State.attrvalue;
                        }
                        else
                        {
                            throw new Exception("expected '='.");
                        }
                        break;

                    case State.attrvalue:
                        if (token is QuotedString)
                        {
                            attributevalue = token;
                            state = State.attrname;

                            result.AddAttribute(attributename, attributevalue);
                        }
                        else
                        {
                            throw new Exception("expected quoted-string.");
                        }
                        break;

                    case State.endelement:
                        if (IsName(token))
                        {
                            result = new XmlEndElement(token);
                            state = State.close;
                        }
                        else
                        {
                            throw new Exception("expected identifier.");
                        }
                        break;

                    case State.close:
                        if (token is AngleBracketRight)
                        {
                            state = State.final;
                        }
                        else
                        {
                            throw new Exception("expected '>'.");
                        }
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }

            result.SetElements(tokens, t => true);
            return result;
        }

        public static XmlNode Text(IEnumerable<Token> tokens)
        {
            var result = new XmlText(tokens);
            result.WithLocation(tokens.FirstOrDefault());
            return result;
        }
    }

    public abstract class XmlNode : Composite
    {
        public string LocalName { get; protected set; }

        public virtual bool HasAttributes { get { return false; } }

        public virtual bool IsEmptyElement { get { return false; } }

        protected XmlNode()
        { }

        protected XmlNode(Token name)
        {
            LocalName = name.Value;
        }

        internal virtual void AddAttribute(Token name, Token value)
        {
            throw new InvalidOperationException();
        }

        internal virtual void SetEmptyElement()
        {
            throw new InvalidOperationException();
        }
    }

    public sealed class XmlAttribute : XmlNode
    {
        public XmlAttribute(Token name, Token value) : base(name)
        {
            _text = value.Value;
        }
    }

    public sealed class XmlElement : XmlNode
    {
        private List<XmlAttribute> _attributes;
        private bool _isempty;

        public override bool HasAttributes { get { return null != _attributes; } }

        public override bool IsEmptyElement { get { return _isempty; } }

        public IEnumerable<XmlAttribute> Attributes
        {
            get { return null == _attributes ? new List<XmlAttribute>() : _attributes; }
        }

        public XmlElement(Token name) : base(name) {}

        internal override void AddAttribute(Token name, Token value)
        {
            if (null == _attributes) _attributes = new List<XmlAttribute>();
            _attributes.Add(new XmlAttribute(name, value));
        }

        internal override void SetEmptyElement()
        {
            _isempty = true;
        }
    }

    public sealed class XmlEndElement : XmlNode
    {
        public XmlEndElement(Token name) : base(name) {}
    }

    public sealed class XmlNotation : XmlNode
    {
        public XmlNotation(Token name) : base(name) { }
    }

    public sealed class XmlText : XmlNode
    {
        public XmlText(IEnumerable<Token> tokens)
        {
            SetElements(tokens);
        }
    }

    public sealed class EntityReference : Composite
    {
        public EntityReference(IEnumerable<Token> tokens) : base(tokens) { }
    }

}
