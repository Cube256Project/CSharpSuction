using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Common
{
    public static class AssemblyExtensions
    {
        private static object _lockobj = new object();
        private static Dictionary<string, string> _map = new Dictionary<string, string>();

        public static Stream GetResourceStream(this Assembly assembly, string suffix)
        {
            var longname = assembly.FullName + ":" + suffix;
            string existingname;

            lock (_lockobj)
            {
                if (!_map.TryGetValue(longname, out existingname))
                {
                    var names = assembly.GetManifestResourceNames()
                        .Where(r => r.EndsWith(suffix));

                    if (!names.Any())
                    {
                        throw new FileNotFoundException("embedded resource '" + suffix + "' not found.");
                    }
                    else if (names.Count() > 1)
                    {
                        throw new Exception("embedded resource '" + suffix + "' ambigous.");
                    }

                    existingname = names.First();
                    _map[suffix] = existingname;
                }
            }

            return assembly.GetManifestResourceStream(existingname);
        }
    }
}
