using System.IO;
using System.Text;

namespace Common.Tokenization
{
    public class TokenWriter
    {
        #region Private

        private StringBuilder _buffer = new StringBuilder();
        private int _indent = 0;
        private bool _ateol = true;
        private int _linechars = 0;

        #endregion

        #region Properties

        public string Text { get { return _buffer.ToString(); } }

        #endregion

        public void Clear()
        {
            _buffer.Clear();
        }

        #region Basic Writing

        public void Write(string text)
        {
            if (_ateol)
            {
                Append(new string(' ', _indent * 4));
                _ateol = false;
            }

            Append(text);
            _linechars += text.Length;
        }

        public void WriteLine()
        {
            AppendLine();
            _ateol = true;
        }

        public void WriteLine(string text)
        {
            if(null != text) Write(text);
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

        public void Indent()
        {
            _indent++;
        }

        public void UnIndent()
        {
            _indent--;
        }

        #endregion

        #region Protected Methods

        protected virtual void Append(string text)
        {
            _buffer.Append(text);
        }

        protected virtual void AppendLine()
        {
            _buffer.AppendLine();
        }

        #endregion
    }
}
