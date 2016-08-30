using System;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    /// <summary>
    /// A labelled tree.
    /// </summary>
    /// <typeparam name="T">The leaf object type.</typeparam>
    public class Tree<T>
    {
        #region Private

        private IEqualityComparer<string> _comparer = null;
        private Dictionary<string, Tree<T>> _children;
        private T _value;

        private bool HasValue
        {
            get
            {
                return !EqualityComparer<T>.Default.Equals(_value, default(T));
            }
        }

        #endregion

        #region Properties

        public T Value { get { return _value; } }

        #endregion

        #region Construction

        public Tree()
            : this(null)
        {
        }

        public Tree(IEqualityComparer<string> comparer)
        {
            if (null == (_comparer = comparer))
            {
                _children = new Dictionary<string, Tree<T>>();
            }
            else
            {
                _children = new Dictionary<string, Tree<T>>(_comparer);
            }
        }

        #endregion

        /// <summary>
        /// Adds or replaces a leaf to the tree.
        /// </summary>
        /// <param name="path">The path to the leaf.</param>
        /// <param name="e">The leaf object or null.</param>
        public void Set(IEnumerable<string> path, T e)
        {
            if (null == path || !path.Any())
            {
                _value = e;
            }
            else
            {
                Tree<T> child;
                var name = path.First();
                if (!_children.TryGetValue(name, out child))
                {
                    _children[name] = child = new Tree<T>(_comparer);
                }

                child.Set(path.Skip(1), e);
            }
        }

        /// <summary>
        /// Retrieves a leaf from the tree.
        /// </summary>
        /// <param name="path">The requested path.</param>
        /// <returns>The leaf value or the default value, if not set.</returns>
        public T Get(IEnumerable<string> path)
        {
            var child = GetChild(path);
            return null == child ? default(T) : child._value;
        }

        public TreeResult<T> GetResult(IEnumerable<string> path)
        {
            var child = GetChild(path);
            if (null != child)
            {
                return new TreeResult<T>(path, child.Value);
            }
            else
            {
                return null;
            }
        }

        public void Clear(IEnumerable<string> path)
        {
            var child = GetChild(path);
            if (null != child) child.Clear();
        }

        /// <summary>
        /// Returns the closure of a path.
        /// </summary>
        /// <param name="path">The base path to search.</param>
        /// <param name="pred">Optional condition.</param>
        /// <returns>Matching child elements.</returns>
        public IEnumerable<TreeResult<T>> Closure(IEnumerable<string> path = null, Func<T, bool> pred = null)
        {
            path = path ?? new string[0];
            var child = GetChild(path);
            if (null != child)
            {
                foreach (var r in child.ClosureInner(path, pred))
                {
                    yield return r;
                }
            }
        }

        public IEnumerable<TreeResult<T>> Children(IEnumerable<string> path = null)
        {
            var e = GetChild(path);

            if(null == e)
            {
                return null;
            }

            var result = new List<TreeResult<T>>();
            foreach (var c in e._children)
            {
                var child = c.Value;
                var value = child._value;
                var childpath = path.Concat(new string[] { c.Key });

                result.Add( new TreeResult<T>(childpath, value));
            }

            return result;
        }

        public IEnumerable<T> Ancestors(IEnumerable<string> path)
        {
            var pe = path.ToArray();
            for (int j = pe.Length; j > 0;)
            {
                j--;

                var child = GetChild(path.Take(j));
                if(child.HasValue)
                {
                    yield return child.Value;
                }
            }
        }

        public void ForAllAlong(IEnumerable<string> path, Action<int, T> action, int depth = 0)
        {
            if(HasValue)
            {
                action(depth, _value);
            }

            if(path.Any())
            {
                Tree<T> child;
                if (_children.TryGetValue(path.First(), out child))
                {
                    child.ForAllAlong(path.Skip(1), action, depth + 1);
                }
            }
        }

        public IEnumerable<Tree<T>> AllAlong(IEnumerable<string> path)
        {
            yield return this;

            if (null != path && path.Any())
            {
                Tree<T> child;
                if (_children.TryGetValue(path.First(), out child))
                {
                    foreach(var c in child.AllAlong(path.Skip(1)))
                    {
                        yield return c;
                    }
                }
            }
        }

        public IEnumerable<T> AllAlongPresent(IEnumerable<string> path)
        {
            foreach (var e in AllAlong(path))
            {
                if(e.HasValue)
                {
                    yield return e.Value;
                }
            }
        }

        public IEnumerable<T> All(Func<T, bool> pred = null)
        {
            if (HasValue && (null == pred || pred(_value)))
            {
                yield return _value;
            }

            foreach (var c in _children)
            {
                foreach (var t in c.Value.All())
                {
                    yield return t;
                }
            }
        }

        public IEnumerable<Tree<T>> AllNodes()
        {
            yield return this;
            foreach (var c in _children)
            {
                foreach(var t in c.Value.AllNodes())
                {
                    yield return t;
                }
            }
        }

        #region Private Methods

        private IEnumerable<TreeResult<T>> ClosureInner(IEnumerable<string> path, Func<T, bool> pred = null)
        {
            foreach(var c in _children)
            {
                var child = c.Value;
                var value = child._value;
                var childpath = path.Concat(new string[] { c.Key });
                var match = child.HasValue ? null == pred || pred(value) : false;

                if (match)
                {
                    yield return new TreeResult<T>(childpath, value);
                }
                else
                {
                    foreach(var e in child.ClosureInner(childpath, pred))
                    {
                        yield return e;
                    }
                }
            }
        }

        private Tree<T> GetChild(IEnumerable<string> path)
        {
            if (null == path || !path.Any())
            {
                return this;
            }
            else
            {
                Tree<T> child;
                if (_children.TryGetValue(path.First(), out child))
                {
                    return child.GetChild(path.Skip(1));
                }
                else
                {
                    return null;
                }
            }
        }

        private void Clear()
        {
            _children.Clear();
        }

        #endregion
    }
}
