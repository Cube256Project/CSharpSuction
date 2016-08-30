using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    /// <summary>
    /// Writes to the system console, with colors.
    /// </summary>
    public class ConsoleWriter
    {
        #region Private

        private static object _globalock = new object();
        private StringBuilder _text = new StringBuilder();
        private bool _ateol = true;
        private int _indent = 0;
        private ConsoleColor _color;
        private ConsoleColor _initialcolor;

        #endregion

        #region Properties

        /// <summary>
        /// Formatted text captured alongside the output.
        /// </summary>
        public string Text { get { return _text.ToString(); } }

        public int TabSize { get; set; }

        public int Width { get; set; }

        public bool Silent { get; set; }

        #endregion

        #region Construction

        public ConsoleWriter()
        {
            TabSize = 3;
            Width = 80;

#if MONO
            _initialcolor = _color = ConsoleColor.Gray;
#else
            _initialcolor = _color = Console.ForegroundColor;
#endif
        }

#endregion

#region Public Methods

        public void SetColor(ConsoleColor color)
        {
            _color = color;
        }

        public void ClearColor()
        {
            _color = _initialcolor;
        }

        public void SetIndent(int indent)
        {
            _indent = indent;
        }

        public void Indent()
        {
            _indent++;
        }

        public void Unindent()
        {
            _indent--;
        }

        public void Write(string text)
        {
            lock (_globalock)
            {
                if (_ateol)
                {
                    var prefix = new string(' ', _indent * TabSize);
                    _text.Append(prefix);

                    if (!Silent)
                    {
                        Console.Write(prefix);
                    }

                    _ateol = false;
                }

                _text.Append(text);

                // TODO: not thread safe, unfortunately ...
                if (!Silent)
                {
#if !MONO
                    var previous = Console.ForegroundColor;
                    Console.ForegroundColor = _color;
                    Console.Write(text);
                    Console.ForegroundColor = previous;
#endif
                }
            }
        }

        public void WriteLine()
        {
            lock (_globalock)
            {
                if (!Silent)
                {
                    Console.WriteLine();
                }

                _text.AppendLine();
                _ateol = true;
            }
        }

        public void WriteLine(string text)
        {
            Write(text);
            WriteLine();
        }

        public void WriteMultiLine(string text)
        {
            var reader = new StringReader(text);
            var first = true;

            while (true)
            {
                var line = reader.ReadLine();
                if (null == line) break;

                if (first) first = false;
                else WriteLine();

                Write(line);
            }
        }

#endregion
    }
}
