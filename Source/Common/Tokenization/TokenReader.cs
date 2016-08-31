using Common;
using Common.ParserFramework;
using Common.Tokenization;
using Common.Tokenization.RuleSets;
using Common.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Common.Tokenization
{
    /// <summary>
    /// Converts a character stream into a token sequence.
    /// </summary>
    /// <remarks>
    /// <para>This class is NOT thread safe.</para>
    /// </remarks>
    public class TokenReader
    {
        #region Private

        private ReductionStack _stack;
        private string _text;
        private int _position;
        private Regex _expression;
        private Queue<Token> _result;
        private LocationTracker _location;
        private bool _ruleschanged;
        private Token _eof;

        #endregion

        #region Properties

        public string BaseURI { get; set; }

        public TokenizerSettings Settings { get; set; }

        #endregion

        #region Construction

        public TokenReader()
        {
            Settings = new TokenizerSettings();
        }

        #endregion

        #region Diagnostics

        public bool Verbose = false;

        // [Conditional("VERBOSE")]
        internal void Trace(string format, params object[] args)
        {
            if (Verbose)
            {
                Log.Trace(format, args);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Reads tokens from a text reader.
        /// </summary>
        /// <param name="reader">The text read to read from.</param>
        /// <returns>An enumeration of tokens.</returns>
        public IEnumerable<Token> Read(TextReader reader)
        {
            _stack = new TokenizerReductionStack(this);
            _result = new Queue<Token>();
            _text = reader.ReadToEnd();
            _position = 0;
            _eof = null;

            _location = new LocationTracker(BaseURI ?? "");

            // install the default rule set
            _stack.PushRules(Settings.DefaultRuleSet ?? new DefaultRuleSet());

            // initial token
            Push(new StartOfFile());

            while (_position < _text.Length)
            {
                // Log.Trace("parse position {0,6}: |{1}|", _position, _text.Substring(_position));

                var match = _expression.Match(_text, _position);

                if (!match.Success)
                {
                    // TODO: this should not happen.
                    throw new Exception("failed to tokenize '" + _text.Substring(_position) + "'.");
                }

                // enumerate expression matches ...
                string gresult = null;
                foreach (var gname in _expression.GetGroupNames().Skip(1))
                {
                    if (match.Groups[gname].Success)
                    {
                        gresult = gname;
                        break;
                    }
                }

                if (null == gresult)
                {
                    throw new Exception("did not match a token group.");
                }

                _location.StartPosition = _position;
                _location.EndPosition = _position + match.Length;

                var token = TokenFactory.ConstructToken(gresult, match)
                    .WithLocation(_location);

                Push(token);

                _position += match.Length;

                _location.IncrementColumn(match.Length);
                if (token is LineBreak)
                {
                    _location.IncrementLine();
                }

                foreach (var e in FlushQueue())
                {
                    // pass control to caller, may change rules
                    yield return e;

                    if (_ruleschanged)
                    {
                        _result.Clear();
                        _ruleschanged = false;
                        break;
                    }
                }
            }

            // final token
            _eof = new EndOfFile().WithLocation(_location);
            Push(_eof);

            // reduce stacks
            while (true)
            {
                _stack.PopRules();
                if (null != _stack.Top)
                {
                    _stack.Reduce(_eof);
                }
                else
                {
                    break;
                }
            }

            Emit(_stack.OfType<Token>());

            foreach (var e in FlushQueue()) yield return e;
        }

        public void PushRuleSet(TokenizerRuleCollection rules, Token restart)
        {
            _stack.PushRules(rules);

            _position = restart.Location.StartPosition;
            _ruleschanged = true;
        }

        public void PopRuleSet(Token restart)
        {
            if (null == _eof)
            {
                _stack.PopRules();
                _position = restart.Location.EndPosition;
                _ruleschanged = true;
            }
        }

        #endregion

        /// <summary>
        /// Emits tokens to the result queue.
        /// </summary>
        /// <param name="tokens">The sequence of tokens to emit.</param>
        internal void Emit(IEnumerable<Token> tokens)
        {
            Trace("<<< ~~~ emit |{0}| ", tokens.ToSeparatorList("|"));

            foreach (var token in tokens)
            {
                _result.Enqueue(token);
            }
        }

        private IEnumerable<Token> FlushQueue()
        {
            while (_result.Any())
            {
                var result = _result.Dequeue();

                if (Settings.OmitWhitespace && result is Whitespace)
                {
                    continue;
                }

                Trace("<<< yield {0,-30} {1}", result.Kind, result);
                yield return result;
            }
        }

        private void Push(Token token)
        {
            _stack.Reduce(token);
            _stack.Push(token);
        }

        internal void OnRulesChanged(TokenizerRuleCollection rules)
        {
            // switch to regular expression specified by the rules
            _expression = rules.Expression;

            Trace("installed rules [{0}] expression: {1}", rules.GetType().Name, _expression);
        }
    }
}
