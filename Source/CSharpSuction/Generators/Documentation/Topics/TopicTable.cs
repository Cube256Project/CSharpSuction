using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSuction.Generators.Documentation.Topics
{
    class TopicTable
    {
        #region Private

        private Tree<TopicTableEntry> _tree = new Tree<TopicTableEntry>();
        private Dictionary<string, TopicTableEntry> _map = new Dictionary<string, TopicTableEntry>();

        #endregion

        public IEnumerable<Topic> GetTopics(TopicReference tref)
        {
            TopicTableEntry entry;
            var set = new HashSet<Topic>();

            entry = _tree.Get(tref.Names);
            if (null != entry)
            {
                foreach (var e in entry) set.Add(e);
            }

            if (!set.Any())
            {
                if (_map.TryGetValue(tref.LastName, out entry))
                {
                    foreach (var e in entry) set.Add(e);
                }
            }

            return set;
        }

        public void AddTopic(Topic topic)
        {
            // the topic key must be a reference
            var tref = TopicReference.Parse(topic.Key);

            if(null != tref.Fragment)
            {
                throw new Exception("cannot register a topic fragment.");
            }

            // qualified name tree
            if(CreateTreeEntry(tref.Names).Add(topic))
            {
                // Log.Debug("added topic {0}.", topic.Key.Quote());
            }

            // simple name table
            CreateMapEntry(tref.LastName).Add(topic);
        }

        private TopicTableEntry CreateMapEntry(string lastName)
        {
            TopicTableEntry entry;
            if (!_map.TryGetValue(lastName, out entry))
            {
                _map.Add(lastName, entry = new TopicTableEntry());
            }

            return entry;
        }

        private TopicTableEntry CreateTreeEntry(IEnumerable<string> key)
        {
            var entry = _tree.Get(key);
            if (null == entry)
            {
                _tree.Set(key, entry = new TopicTableEntry());
            }

            return entry;
        }
    }
}
