using Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CSharpSuction
{
    /// <summary>
    /// Maintains a map from interface types with ObjectFactory attribute 
    /// to implementation types. An interface may have multiple implementations.
    /// </summary>
    public class FactorySelection
    {
        #region Private

        private class List : List<string>
        {
            public bool Applied { get; set; }
        }

        private Dictionary<string, List> _map = new Dictionary<string, List>();

        #endregion

        public int Count { get { return _map.Count; } }

        /// <summary>
        /// Returns the full names of interfaces that have more than one implementation choice.
        /// </summary>
        public IEnumerable<string> AmbigousEntries
        {
            get
            {
                return _map
                  .Where(e => e.Value.Count > 1)
                  .Select(e => e.Key)
                  .ToList();
            }
        }

        public IEnumerable<string> UnappliedImplementations
        {
            get
            {
                foreach (var entry in _map.Values.Where(e => !e.Applied))
                {
                    yield return entry.First();
                }
            }
        }

        /// <summary>
        /// Adds an implementation for an interface.
        /// </summary>
        /// <param name="ifname">The full name of the interface type.</param>
        /// <param name="implname">The full name of the implementation type.</param>
        public void Add(string ifname, string implname)
        {
            List list;
            if (!_map.TryGetValue(ifname, out list))
            {
                _map[ifname] = list = new List();
            }

            list.Add(implname);

            Log.Trace("  {0} -> {1}", ifname, implname);
        }

        public IEnumerable<FactoryChoice> GetMapping(string interfacetypename)
        {
            List list;
            if (_map.TryGetValue(interfacetypename, out list))
            {
                return list.Select(e => new FactoryChoice
                {
                    InterfaceTypeName = interfacetypename,
                    ImplementationTypeName = e
                })
                .ToList();
            }
            else
            {
                return new FactoryChoice[0];
            }
        }

        public IEnumerable<FactoryChoice> GetChosenMappings()
        {
            foreach (var pair in _map)
            {
                if (pair.Value.Count > 1)
                {
                    throw new Exception("interface [" + pair.Key + "] has multiple implementations.");
                }

                yield return new FactoryChoice
                {
                    InterfaceTypeName = pair.Key,
                    ImplementationTypeName = pair.Value.First()
                };
            }
        }

        public void ChooseMapping(FactoryChoice mapping)
        {
            _map.Remove(mapping.InterfaceTypeName);
            Add(mapping.InterfaceTypeName, mapping.ImplementationTypeName);
        }

        public void SetApplied(string implname)
        {
            foreach (var pair in _map)
            {
                if (pair.Value.Contains(implname))
                {
                    pair.Value.Applied = true;
                }
            }
        }
    }
}
