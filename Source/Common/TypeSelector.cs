using System;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    /// <summary>
    /// Selects types.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is contained in the selection.</returns>
    public delegate bool TypeSelector(Type type);

    public static class TypeSelectorFactory
    {
        public static TypeSelector CreateExactMatch(Type type)
        {
            return new TypeSelector(t => t == type);
        }

        public static TypeSelector CreateClassMatch(Type type)
        {
            return new TypeSelector(t => type.IsAssignableFrom(t));
        }

        public static TypeSelector CreateClassMatch(params Type[] types)
        {
            return new TypeSelector(t => types.Any(u => u.IsAssignableFrom(t)));
        }

        public static TypeSelector CreateExactMatch(params Type[] type)
        {
            var set = new HashSet<Type>(type);
            return new TypeSelector(t => set.Contains(t));
        }

    }
}
