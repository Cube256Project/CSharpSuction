using Common;
using Common.Tokenization;
using Common.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CSharpSuction.Generators.Documentation.Topics
{
    /// <summary>
    /// Refers to a topic or fragment within a topic.
    /// </summary>
    class TopicReference
    {
        private static Regex _extensionfilter = new Regex(@".html$");

        public string[] Names { get; private set; }

        public string Fragment { get; private set; }

        public string LastName {  get { return Names.Last(); } }

        public IEnumerable<string> Namespaces
        {
            get
            {
                for (int j = Names.Length; j > 0; --j)
                {
                    yield return Names.Take(j).ToSeparatorList(".");
                }
            }
        }

        private TopicReference(string[] names)
        {
            Names = names;
        }

        public override string ToString()
        {
            var result = Names.ToSeparatorList(".");

            if(null != Fragment)
            {
                result += "#" + Fragment;
            }

            return result;
        }

        public static TopicReference Parse(string cref)
        {
            cref = _extensionfilter.Replace(cref, m => string.Empty);
            string[] names = null;

            int s = 1;
            foreach (var token in new TokenReader().Read(cref))
            {
                if (s == 1)
                {
                    if(token is StartOfFile)
                    {
                        continue;
                    }
                    else if (token is GeneralString)
                    {
                        names = token
                            .ToElements(new TypeSelector(t => typeof(DottedString).IsAssignableFrom(t)))
                            .Where(t => typeof(GeneralString).IsAssignableFrom(t.GetType()))
                            .Select(t => t.Value)
                            .ToArray();

                        s = 2;
                    }
                    else
                    {
                        throw new TokenReferenceFormatException("expected dotted-string");
                    }
                }
                else if (s == 2)
                {
                    if (token is EndOfFile)
                    {
                        s = 99;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            if(null == names)
            {
                throw new TokenReferenceFormatException("failed to parse token reference.");
            }

            return new TopicReference(names);
        }
    }
}
