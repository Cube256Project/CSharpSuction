using Common.Tokenization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSuction.Generation
{
    abstract class ScriptStateModifier
    {
    }

    class ThisRedirectModifier : ScriptStateModifier
    {
        public string ThisName { get; private set; }

        public ThisRedirectModifier(string altname)
        {
            ThisName = altname;
        }
    }

    class WriterRedirectModifier : ScriptStateModifier
    {
        public TokenWriter Writer { get; private set; }

        public WriterRedirectModifier(TokenWriter writer)
        {
            Writer = writer;
        }
    }

    class EventAssignmentModifier : ScriptStateModifier
    { }

    class InitializerModifier : ThisRedirectModifier
    {
        public string AlternativeLeft { get; private set; }

        public InitializerModifier(string left, string right) : base(right)
        {
            AlternativeLeft = left;
        }
    }
}
