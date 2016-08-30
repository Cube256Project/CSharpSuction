using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSuction.Documentation
{
    class HtmlDocumentationCallback
    {
        private HtmlDocumentationGenerator _gen;

        public HtmlDocumentationCallback(HtmlDocumentationGenerator gen)
        {
            _gen = gen;
        }

        public string TranslateReference(string cref)
        {
            return _gen.TranslateReference(cref);
        }

    }
}
