using Common;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace CSharpSuction.Input
{
    /// <summary>
    /// Makes simple names to a set of qualified names.
    /// </summary>
    class NameMap
    {
        #region Private

        private class List : List<NameInfo> { }

        private Dictionary<string, List> _map = new Dictionary<string, List>();

        #endregion

        #region Public Methods

        public void Add(string name, NameRole role, SourceInfo source, SyntaxNode node)
        {
            var entry = new NameInfo
            {
                Source = source,
                Role = role,
                Node = node
            };

            List list;
            if(!_map.TryGetValue(name, out list))
            {
                _map[name] = list = new List();
            }

            list.Add(entry);
        }

        public IEnumerable<NameInfo> LookupName(string name)
        {
            List list;
            if (name.Contains('*'))
            {
                list = new List();
                var rx = WildcardFactory.BuildWildcardsFromList(name);
                list.AddRange(_map.Where(e => rx.IsMatch(e.Key)).SelectMany(e => e.Value));
                return list;
            }
            else if (_map.TryGetValue(name, out list))
            {
                return list;
            }
            else
            {
                return new NameInfo[0];
            }
        }

        #endregion
    }
}
