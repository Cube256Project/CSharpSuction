using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CSharpSuction.Input
{
    public class NuGetReferenceInfo : AssemblyReferenceInfo
    {
        public string PackageName { get; private set; }

        public string Version { get; private set; }

        public string HintPath { get; private set; }

        public override string Key { get { return "cfs:nuget-package:" + PackageName + ":" + Location; } }

        public NuGetReferenceInfo(string package, string assemblyname)
        {
            PackageName = package;
            Location = assemblyname;
        }

        public override string GetFullPath()
        {
            // trickery?? .. where is it

            // TODO: look in packages
            var folder = @"C:\Users\tc\Repositories\igra3\packages";

            var pname = PackageName.Replace(".", @"\.");
            var aname = Location.Replace(".", @"\.");

            var rx = new Regex("^" + pname + @"(?<version>\.([\w\d\.\-])+)" + @"/lib/(net45|dotnet|portable\-net45\+win8)/" + Location + ".*$");

            // Log.Debug("search assembly {0} expression [{1}] ...", PackageName.Quote(), rx);

            string result = null;
            foreach (var ca in RecursePath(folder, rx))
            {
                // Log.Debug("  {0}", ca);

                result = ca;
            }

            if(null == result)
            {
                throw new Exception("package DLL " + pname.Quote() + " was not found in " + folder.Quote() + ".");
            }

            var match = rx.Match(result);

            Version = match.Groups["version"].Value.Substring(1);

            HintPath = result;

            Log.Information("using nuget package {0}.{1}.", PackageName, Version);

            return Path.Combine(folder, result);
        }

        private IEnumerable<string> RecursePath(string folder, Regex rx, params string[] sub)
        {
            // TODO: lexical ordering?
            foreach (var dir in Directory.GetDirectories(folder))
            {
                var name = Path.GetFileName(dir);
                var g = sub.Concat(new[] { name }).ToArray();
                foreach (var c in RecursePath(dir, rx, g))
                {
                    yield return c;
                }
            }

            foreach (var file in Directory.GetFiles(folder))
            {
                var name = Path.GetFileName(file);
                var qual = sub.Concat(new[] { name }).ToPath();

                if (rx.IsMatch(qual))
                {
                    yield return qual;
                }
            }
        }
    }
}
