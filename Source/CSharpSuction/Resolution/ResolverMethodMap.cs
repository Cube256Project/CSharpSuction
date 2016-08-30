using System;
using System.Collections.Generic;
using System.Reflection;

namespace CSharpSuction.Resolution
{
    class ResolverMethodMap
    {
        private Dictionary<Type, MethodInfo> _map = new Dictionary<Type, MethodInfo>();

        public MethodInfo GetSyntaxNodeMethod(string prefix, object node)
        {
            MethodInfo result = null;
            Type argument = node.GetType();
            while (null != argument)
            {
                if (_map.TryGetValue(argument, out result))
                {
                    break;
                }
                else if(TryFindMethod(prefix, argument, out result))
                {
                    _map.Add(argument, result);
                    break;
                }
                else
                {
                    argument = argument.BaseType;
                }
            }

            if(null == result)
            {
                throw new Exception("resolver method not found for [" + node.GetType().Name + "].");
            }

            return result;
        }

        private bool TryFindMethod(string prefix, Type type, out MethodInfo method)
        {
            var name = prefix + type.Name;
            method = typeof(Resolver).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            return null != method;
        }
    }
}
