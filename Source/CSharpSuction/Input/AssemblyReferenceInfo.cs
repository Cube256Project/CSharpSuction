using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common;

namespace CSharpSuction.Input
{
    public abstract class AssemblyReferenceInfo
    {
        public string Location { get; protected set; }

        public Assembly Assembly { get; protected set; }

        public abstract string Key { get; }

        protected AssemblyReferenceInfo()
        { }

        public override string ToString()
        {
            return Key;
        }

        public virtual string GetFullPath()
        {
            if (null != Assembly)
            {
                return Assembly.Location;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }

    public class ArgumentReferenceInfo : AssemblyReferenceInfo
    {
        public override string Key {  get { return Location; } }

        public ArgumentReferenceInfo(string l)
        {
            Location = l;
        }

        public override string GetFullPath()
        {
            return Location;
        }
    }

    public class SystemReferenceInfo : AssemblyReferenceInfo
    {
        public override string Key
        {
            get
            {
                return "system:" + Location;
            }
        }

        public SystemReferenceInfo(string name, Assembly a)
        {
            Assembly = a;

            // TODO: location from GAC?
            Location = name;
        }
    }

    public class RuntimeReferenceInfo : AssemblyReferenceInfo
    {
        public override string Key { get { return Location; } }

        public RuntimeReferenceInfo(Assembly a)
        {
            Assembly = a;
            Location = a.Location;
        }
    }

    }
