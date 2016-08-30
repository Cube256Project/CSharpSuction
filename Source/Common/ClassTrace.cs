using System;
using System.Collections.Generic;

namespace Common
{
    /// <summary>
    /// Implements a per-class trace switch based on <see cref="LogContext"/>
    /// and <see cref="Log"/>.
    /// </summary>
    public static class ClassTrace
    {
        #region

        private static object _lockobj = new object();
        private static HashSet<Type> _types = new HashSet<Type>();

        #endregion

        public static void SetTrace(this Type classtype, bool enable = true)
        {
            lock (_lockobj)
            {
                if (enable)
                {
                    if (!_types.Contains(classtype))
                    {
                        _types.Add(classtype);
                    }
                }
                else
                {
                    _types.Remove(classtype);
                }
            }
        }

        public static bool IsTraceEnabled(this Type classtype)
        {
            lock (_lockobj)
            {
                var first = true;
                while (null != classtype)
                {
                    if (_types.Contains(classtype))
                    {
                        return true;
                    }

                    if (first)
                    {
                        first = false;
                        foreach (var it in classtype.GetInterfaces())
                        {
                            if (_types.Contains(it))
                            {
                                return true;
                            }
                        }
                    }

                    classtype = classtype.BaseType;
                }

                return false;
            }
        }
    }
}
