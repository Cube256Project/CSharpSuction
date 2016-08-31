using System.Collections.Generic;
using System.IO;

namespace Common
{
    public static class PathBuilderExtensions
    {
        public static string[] SplitPath(this string s)
        {
            return PathBuilder.Parse(s).ToArray();
        }

        public static string CombinePath(this IEnumerable<string> elist)
        {
            return PathBuilder.CombineNames(elist);
        }

        public static string MakeAbsolutePath(this string path)
        {
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), path);
            }

            return Path.GetFullPath(path);
        }
    }
}
