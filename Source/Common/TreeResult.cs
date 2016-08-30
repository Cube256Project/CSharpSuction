using System.Collections.Generic;

namespace Common
{
    public class TreeResult<T>
    {
        public IEnumerable<string> Path { get; set; }

        public T Value { get; set; }

        internal TreeResult(IEnumerable<string> path, T value)
        {
            Path = path;
            Value = value;
        }
    }
}
