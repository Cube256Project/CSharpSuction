using System;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    /// <summary>
    /// The elements of a path.
    /// </summary>
    public class PathElements : List<string>
    {
        public PathElements(string path)
        {
            if (null != path)
            {
                var e0 = path.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                var e1 = e0.Select(e => e.Trim()).Where(e => e.Length > 0 && e != ".");
                if(e1.Any(e => e == ".."))
                {
                    throw new ArgumentException("parent directory operator '..' not allowed in this context.");
                }

                this.AddRange(e1);
            }
        }

        public string GetFileName()
        {
            if (Count > 0)
            {
                return this.Last();
            }
            else
            {
                throw new Exception("specified path is empty.");
            }
        }

        public string GetDirectoryName()
        {
            if (Count > 0)
            {
                return PathBuilder.CombineNames(this.Take(Count - 1));
            }
            else
            {
                throw new Exception("specified path is empty.");
            }
        }

        /// <summary>
        /// Returns the normalized relative path corresponding to this element list.
        /// </summary>
        /// <returns></returns>
        public string GetPath()
        {
            return PathBuilder.CombineNames(this);
        }
    }
}
