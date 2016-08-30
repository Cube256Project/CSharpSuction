using CSharpSuction.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Common;
using System.Reflection;

namespace CSharpSuction.Toolmap
{
    class ToolmapReader : IToolmap
    {
        private XmlNamespaceManager nm;
        private XmlDocument dom;

        public IToolmap Load(Stream stream)
        {
            dom = new XmlDocument();
            dom.Load(stream);

            nm = new XmlNamespaceManager(dom.NameTable);
            nm.AddNamespace("t", "cfs:schema:suction:toolmap:1611");

            return this;
        }

        public IEnumerable<AssemblyReferenceInfo> GetReferenceViaNamespace(string ns)
        {
            foreach (var e in dom.SelectNodes("/t:toolmap/t:imply[starts-with(@namespace, " + ns.Quote() + ")]", nm).OfType<XmlElement>())
            {
                var list = new List<AssemblyReferenceInfo>();
                foreach (var q in e.SelectNodes("t:package", nm).OfType<XmlElement>())
                {
                    if (q.HasAttribute("ref"))
                    {
                        list.AddRange(ProcessPackageReference(q.GetAttribute("ref")));
                    }
                    else
                    {
                        list.AddRange(ProcessPackage(q));
                    }
                }

                if (list.Any())
                {
                    foreach (var u in list)
                    {
                        yield return u;
                    }
                }
                else
                {
                    // assume system
                    var name = ns;
                    if (e.HasAttribute("library"))
                    {
                        name = e.GetAttribute("library");
                    }

                    Assembly u = null;
                    foreach (var x in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        var lname = x.GetName().Name;
                        // Log.Debug("looking {0} {1}", lname.Quote(), name.Quote());
                        if (string.Equals(name, lname))
                        {
                            u = x;
                            break;
                        }
                    }

                    if (null != u)
                    {
                        yield return new SystemReferenceInfo(name, u);
                    }
                    else
                    {
                        Log.Warning("assembly {0} not found.", name.Quote());

                        try
                        {
                            Assembly.Load(name);
                        }
                        catch (Exception ex)
                        {
                            Log.CreateWarning().WithHeader("unable to load assembly '{0}'", name)
                                .WithData(ex)
                                .Submit();
                        }
                    }
                }
            }
        }

        private IEnumerable< AssemblyReferenceInfo> ProcessPackage(XmlElement e)
        {
            if (e.HasAttribute("name"))
            {
                var packagename = e.GetAttribute("name");
                foreach (var a in e.SelectNodes("t:assembly", nm).OfType<XmlElement>())
                {
                    if (a.HasAttribute("location"))
                    {
                        yield return new NuGetReferenceInfo(packagename, a.GetAttribute("location"));
                    }
                    else
                    {
                        throw new Exception("missing 'location' attribute on 'assembly'.");
                    }
                }
            }
            else
            {
                throw new Exception("missing 'name' in package.");
            }
        }

        private IEnumerable<AssemblyReferenceInfo> ProcessPackageReference(string v)
        {
            foreach (var e in dom.SelectNodes("/t:toolmap/t:package[@name=" + v.Quote() + "]", nm).OfType<XmlElement>())
            {
                foreach(var u in ProcessPackage(e))
                {
                    yield return u;
                }
            }
        }
    }
}
