using System;
using System.Diagnostics;
using System.Linq;

namespace Common.ParserFramework
{
    public class PredicateFactory
    {
        #region Predicate Factories

        public static Predicate MatchAny()
        {
            return new Predicate(r => true);
        }

        public static Predicate ExactType(Type type)
        {
            // no sense
            Debug.Assert(!type.IsAbstract);

            return new Predicate(r => r.GetType() == type);
        }

        public static Predicate ClassType(Type type)
        {
            return new Predicate(r => type.IsAssignableFrom(r.GetType()));
        }

        public static Predicate ClassTypes(params Type[] types)
        {
            return new Predicate(r => types.Any(t => t.IsAssignableFrom(r.GetType())));
        }

        #endregion
    }
}
