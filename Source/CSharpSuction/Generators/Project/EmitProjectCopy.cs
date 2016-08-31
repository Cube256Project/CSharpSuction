using Common;
using CSharpSuction.Input;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSharpSuction.Generators.Project
{
    /// <summary>
    /// Emit a copy of the project, arranged by namespaces.
    /// </summary>
    public class EmitProjectCopy : EmitProject
    {
        #region Private

        private Dictionary<string, string> _copies = new Dictionary<string, string>();

        #endregion

        protected override string TranslatePath(ISourceInfo source)
        {
            return base.TranslatePath(CopyProjectSource(source));
        }

        /// <summary>
        /// Copies a source file into its destination location given by namespace.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        protected virtual string CopyProjectSource(ISourceInfo source)
        {
            if (_copies.ContainsKey(source.FullPath))
            {
                Log.Warning("already contains {0}", source.FullPath.Quote());
                return _copies[source.FullPath];
            }

            var filename = Path.GetFileName(source.FullPath);

            // path relative to original project
            var reldir = Path.GetDirectoryName(MakeProjectRelativePath(source.FullPath, OriginalDirectory));

            SyntaxTree tree;
            if (source.TryGetTree(out tree))
            {
                // relative directory by namespace
                var implicators = new SourceImplictors();
                FindImplicators(tree.GetRoot(), implicators);

                if (implicators.Namespaces.Count > 1)
                {
                    throw new Exception(source.FullPath + ": multiple namespace in once source are not allowed for [EmitProject].");
                }

                var logicallist = implicators.Namespaces.First().Split('.');

                // TODO: HACK: the 'Source' could/should come from somewhere...
                reldir = "Source/" + logicallist.ToPath();
            }
            else
            {
                // just copy
            }


            // construct absoluted location in the output directory
            var absdir = Path.Combine(OutputBaseDirectory, reldir);
            Directory.CreateDirectory(absdir);
            var absfile = Path.Combine(absdir, filename);

            // Log.Debug("copy {0} -> {1} ...", source.FullPath, absfile);

            try
            {
                if (File.Exists(absfile))
                {
                    // protect from overwriting modified stuff
                    if (File.GetLastWriteTimeUtc(absfile) > File.GetLastWriteTimeUtc(source.FullPath))
                    {
                        throw new Exception("file '" + absfile + "' exists and has modification time after source; delete manually to resolve.");
                    }
                }

                File.Copy(source.FullPath, absfile, true);

                // copy last access time as well for check above
                File.SetLastAccessTime(absfile, File.GetLastAccessTime(source.FullPath));
            }
            catch (Exception ex)
            {
                Log.Debug("failed to copy {0}: {1}", absdir, ex.Message);

                throw;
            }

            // remember
            _copies.Add(source.FullPath, absfile);

            return absfile;
        }

        protected override void OnAfterProjectFileGenerated()
        {
            EmitSolutionFile();
        }

        private static string MergeLogicalPath(string reldir, string[] logicallist)
        {
            var result = new List<string>();
            var arglist1 = PathBuilder.Parse(reldir);

            var arglist = arglist1.SelectMany(e => e.Split('.'));

            var u = arglist.GetEnumerator();
            var l = logicallist.OfType<string>().GetEnumerator();
            var m = 0;
            string uname = null;
            string lname = null;
            while (true)
            {
                if (null == uname && u.MoveNext())
                {
                    uname = u.Current;
                }

                if (null == lname && l.MoveNext())
                {
                    lname = l.Current;
                }

                if (null == uname)
                {
                    break;
                }

                if (m == 0)
                {
                    if (uname != lname)
                    {
                        result.Add(uname);
                        uname = null;
                    }
                    else
                    {
                        result.Add(lname);
                        lname = null;
                        m = 1;
                    }
                }
                else if (null != lname)
                {
                    result.Add(lname);
                    lname = null;
                }
                else
                {
                    break;
                }
            }

            if (0 == m)
            {
                // TODO: hackachkkkk.
                reldir = "Source/" + logicallist.ToPath();
            }
            else
            {
                reldir = result.ToPath();
            }
            return reldir;
        }
    }
}
