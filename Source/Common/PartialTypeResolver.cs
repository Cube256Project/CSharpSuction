using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Common
{
    /// <summary>
    /// Keeps track of types in loaded assemblies.
    /// </summary>
    public static class PartialTypeResolver
    {
        #region Private

        private static object _lockobj = new object();
        private static HashSet<Assembly> _assemblies = new HashSet<Assembly>();
        private static Dictionary<string, Type> _globaltypes = new Dictionary<string, Type>();

        #endregion

        #region Construction

        static PartialTypeResolver()
        {
            lock (_lockobj)
            {
                // install a listener for the assembly load event
                AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;

                // process loaded assemblies
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (_assemblies.Add(a))
                    {
                        LoadAssemblyTypes(a);
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Performs a suffix search on a given name.
        /// </summary>
        /// <param name="suffix">The suffix to search.</param>
        /// <param name="types">The set of types to search from.</param>
        /// <returns></returns>
        public static Type Resolve(string suffix, IEnumerable<Type> types = null)
        {
            Type result;

            if(null == suffix)
            {
                throw new ArgumentNullException("suffix");
            }

            var parts = suffix.Split(new char[] { ',' }, 2, StringSplitOptions.RemoveEmptyEntries);

            // TODO: handle assembly part (for collisions).
            suffix = parts[0];

            lock (_lockobj)
            {
                types = types ?? _globaltypes.Values;

                if (!_globaltypes.TryGetValue(suffix, out result))
                {
                    // TODO: improve performance?
                    types = types
                        .Where(t => MatchSuffix(suffix, t.FullName));

                    var it = types.GetEnumerator();
                    if (!it.MoveNext())
                    {
                        throw new TypeResolverException("type with suffix '" + suffix + "' was not found.");
                    }

                    result = it.Current;
                    if (it.MoveNext())
                    {
                        throw new TypeResolverException("multiple types with suffix '" + suffix + "' found: " + types.ToSeparatorList());
                    }
                }
            }

            return result;
        }

        public static IEnumerable<Type> GetAllTypes()
        {
            lock (_lockobj)
            {
                return _globaltypes.Values.ToList();
            }
        }

        #endregion

        #region Private Methods

        private static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            lock (_lockobj)
            {
                var a = args.LoadedAssembly;
                if (_assemblies.Add(a))
                {
                    LoadAssemblyTypes(a);
                }
            }
        }

        private static void LoadAssemblyTypes(Assembly a)
        {
            if (a.GlobalAssemblyCache || a.IsDynamic)
            {
                // TODO: add only application types.
                return;
            }

            try
            {

                foreach (var t in a.GetTypes().Where(t => t.IsClass || t.IsInterface))
                {
                    _globaltypes[t.FullName] = t;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("failed to process assemble [" + a.FullName + "]: " + ex.Message);
            }

            // Debug.WriteLine("[PartialTypeResolver] loaded {0}, now {1} types.", a.GetName().Name, _globaltypes.Count);
        }

        private static bool MatchSuffix(string suffix, string argument)
        {
            var sparts = suffix.Split(new char[] { '.', '+' });
            var aparts = argument.Split(new char[] { '.', '+' });

            var sit = sparts.Reverse().GetEnumerator();
            var ait = aparts.Reverse().GetEnumerator();

            while (sit.MoveNext())
            {
                if (!ait.MoveNext()) return false;
                if (0 != string.Compare(sit.Current, ait.Current)) return false;
            }

            return true;
        }

        #endregion

    }
}
