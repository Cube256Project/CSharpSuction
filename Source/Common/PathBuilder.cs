using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common
{
    /// <summary>
    /// Constructs and parses directory path.
    /// </summary>
    /// <remarks>
    /// <para>Uses the slash '/' as a path separator.</para>
    /// </remarks>
    public static class PathBuilder
    {
        #region Public Methods

        /// <summary>
        /// Combines a set of names into a path.
        /// </summary>
        /// <param name="names">List of names.</param>
        /// <param name="separator">Optional path separator.</param>
        /// <returns>The concatenation of the names separated by the given separator.</returns>
        public static string CombineNames(IEnumerable<string> names, string separator = "/")
        {
            StringBuilder sb = new StringBuilder();
            foreach (string name in names)
            {
                if (sb.Length > 0) sb.Append(separator);
                sb.Append(name);
            }

            return sb.ToString();
        }

        public static string Combine(params string[] elements)
        {
            var e = elements
                .Select(u => new PathElements(u))
                .SelectMany(g => g)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0);

            return CombineNames(e);
        }

        public static string CombineToAbsolute(params string[] elements)
        {
            return "/" + Combine(elements);
        }

        public static PathElements Parse(string path)
        {
            return new PathElements(path);
        }

        public static string GetFileName(string path)
        {
            return new PathElements(path).LastOrDefault();
        }

        public static string GetDirectoryName(string path)
        {
            return new PathElements(path).GetDirectoryName();
        }

        public static string Normalize(string path)
        {
            return PathBuilder.Parse(path).GetPath();
        }

        public static string CombineURL(string absolute, string relpath, string qs = null)
        {
            var pe = Parse(relpath);
            absolute = absolute.Trim();

            var sb = new StringBuilder();
            var flag = absolute.EndsWith("/");
            sb.Append(absolute);
            if (!flag) sb.Append("/");
            if (pe.Count > 0)
            {
                sb.Append(pe.GetPath());
            }

            if(null != qs)
            {
                sb.Append("?");
                sb.Append(qs);
            }

            return sb.ToString();
        }

        public static string SubstractContainerPath(string contpath, string vpath)
        {
            if(!vpath.StartsWith(contpath))
            {
                throw new ArgumentException("given path is not part of given container.");
            }

            var rpath = vpath.Substring(contpath.Length);
            rpath = rpath.Trim(new char[] { '/', '\\' });

            return rpath;
        }

        #endregion
    }
}
