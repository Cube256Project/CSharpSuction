using CSharpSuction.Input;
using System.Collections.Generic;
using System.Web;

namespace CSharpSuction
{
    public class UsingImplicator
    {
        private IToolmap _toolmap;

        public UsingImplicator(IToolmap toolmap)
        {
            _toolmap = toolmap;
        }

        public IEnumerable<AssemblyReferenceInfo> GetAssemblyLocations(string ns)
        {
            //var result = new List<AssemblyReferenceInfo>();

            //throw new NotImplementedException();

            if (ns == "System.Data")
            {

            }
            else if (ns.StartsWith("System.Web"))
            {
                var umm4 = new HtmlString("foo");
            }

            return _toolmap.GetReferenceViaNamespace(ns);
#if FOO
            /*if(ns == "System.Linq")
            {
                result.Add("System.Linq.dll");
            }
            else if (ns == "System.Data")
            {
                result.Add("System.Data.dll");
            }
            else if (ns == "System.Numerics")
            {
                result.Add("System.Numerics.dll");
            }
            else if (ns == "System.Xml" || ns.StartsWith("System.Xml."))
            {
                result.Add("System.Xml.dll");
            }
            else if (ns == "System.Text.RegularExpressions")
            {
                result.Add("System.Text.RegularExpressions.dll");
            }*/


            if (ns == "Newtonsoft.Json")
            {
                //result.Add(typeof(Newtonsoft.Json.JsonConvert).Assembly.Location);
            }
            else if (ns == "System.Data.SqlServerCe")
            {
                result.Add(@"C:\Program Files (x86)\Microsoft SQL Server Compact Edition\v4.0\Desktop\System.Data.SqlServerCe.dll");
            }
            else if (ns == "System.Data.Common" || ns == "System.Data")
            {
                result.Add(typeof(DbCommand).Assembly.Location);
                result.Add(typeof(System.Data.CommandType).Assembly.Location);
            }
            else if (ns == "System.Xml" || ns == "System.Xml.Serialization")
            {
                result.Add(typeof(XmlReader).Assembly.Location);
            }
            else if (ns == "System.Numerics")
            {
                result.Add(typeof(BigInteger).Assembly.Location);
            }
            else if (ns == "System.Security.Cryptography")
            {
                result.Add(typeof(SignedCms).Assembly.Location);
            }
            else if (ns == "System.IO.Packaging")
            {
                result.Add(typeof(System.IO.Packaging.PackUriHelper).Assembly.Location);
            }
            else if (ns == "System.Configuration")
            {
                result.Add(typeof(System.Configuration.Configuration).Assembly.Location);
            }
            else if (ns == "System.Web")
            {
                result.Add(typeof(System.Web.HttpContextBase).Assembly.Location);
            }
            else if (ns == "System.Windows")
            {
                result.Add(typeof(System.Windows.Clipboard).Assembly.Location);
                result.Add(typeof(System.Windows.Application).Assembly.Location);
                result.Add(typeof(System.Windows.DependencyObject).Assembly.Location);
            }
            else if (ns == "System.Windows.Controls")
            {
                result.Add(typeof(System.Windows.Controls.Control).Assembly.Location);
            }
            else if (ns == "System.Windows.Markup")
            {
                result.Add(typeof(System.Windows.Markup.IQueryAmbient).Assembly.Location);
            }
            else if (ns == "System.Windows.Forms")
            {
                result.Add(typeof(System.Drawing.Brush).Assembly.Location);
                result.Add(typeof(System.Windows.Forms.Application).Assembly.Location);
                result.Add(typeof(System.Xaml.XamlObjectWriter).Assembly.Location);
            }
            else if (ns.StartsWith("Microsoft.VisualStudio."))
            {
                result.Add(@"C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\PublicAssemblies\Microsoft.VisualStudio.QualityTools.UnitTestFramework.dll");
            }
            

            return result;
#endif
        }
    }
}
