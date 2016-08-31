using Common;
using System.Collections.Generic;
using System.Collections;

namespace CSharpSuction.Generators.Documentation.Topics
{
    class TopicLinkCollection : IEnumerable<TopicLink>
    {
        private HashSet<TopicLink> _set = new HashSet<TopicLink>();

        public void Add(TopicLink link)
        {
            if (_set.Add(link))
            {
                // Log.Debug("[AddRelation] {0} => {1} => {2}", link.ReferingTopic, link.Kind.GetType().Name, link.ReferencedTopic);
            }
        }

        public IEnumerator<TopicLink> GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _set.GetEnumerator();
        }
    }
}
