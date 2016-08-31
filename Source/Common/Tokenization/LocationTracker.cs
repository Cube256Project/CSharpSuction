
namespace Common.Tokenization
{
    public class LocationTracker : ITokenLocation
    {
        #region Private

        private string _baseuri;
        private int _line;
        private int _column;
        private int _position;

        #endregion

        #region Properties

        public string BaseUri { get { return _baseuri; } }

        public int Line { get { return _line; } }

        public int Column { get { return _column; } }

        public int StartPosition { get { return _position; } set { _position = value; } }

        public int EndPosition { get; set; }

        #endregion

        public LocationTracker(string baseuri)
        {
            _baseuri = baseuri;
        }

        public void IncrementLine()
        {
            _line++;
            _column = 0;
            _position++;
        }

        public void IncrementColumn(int p)
        {
            _column += p;
            _position += p;
        }
    }
}
