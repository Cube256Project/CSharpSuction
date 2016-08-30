using System.Collections.Generic;
using System.Xml;

namespace CSharpSuction.Documentation
{
    /// <summary>
    /// Represents an element in the topic tree.
    /// </summary>
    class Topic
    {
        public List<XmlElement> Nodes = new List<XmlElement>();

        public readonly string Key;

        public string Title;

        /// <summary>
        /// The 'cfs:docid' 
        /// </summary>
        public string DocID;

        public Topic(string key)
        {
            Key = key;
            Title = key;
        }
    }
}
