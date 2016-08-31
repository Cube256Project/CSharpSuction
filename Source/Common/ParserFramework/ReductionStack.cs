using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.ParserFramework
{
    /// <summary>
    /// A sequence of objects reduction rules operate on.
    /// </summary>
    /// <remarks>
    /// <para>This class may serve as a base-class for specific parsers or be used as is.</para>
    /// <para>The reduction stack operates with a <see cref="ReductionRuleCollection"/>, which may be 
    /// adjusted via <see cref="PushRules"/> during parsing.</para>
    /// <para>The kind of object stored on the stack depends on the application. If can be tokens,
    /// productions or state information.</para>
    /// <para>Objects on the stack can be retrieved via index. Negative indexes are counted from right,
    /// with -1 being the index of the rightmost element.</para>
    /// </remarks>
    public class ReductionStack : IReadOnlyList<object>
    {
        #region Private
        
        private List<object> _stack = new List<object>();
        private Stack<ReductionRuleCollection> _rulestack = new Stack<ReductionRuleCollection>();
        private bool _reducebreak;

        #endregion

        #region Properties

        /// <summary>
        /// Read access to stack elements.
        /// </summary>
        /// <param name="index">The index of the element. Negative values count from right.</param>
        /// <returns>The object or null.</returns>
        public object this[int index]
        {
            get
            {
                if (index < 0)
                {
                    index = Count + index;
                }

                if(0 <= index && index < _stack.Count)
                {
                    return _stack[index];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// The number of elements currently on the stack.
        /// </summary>
        public int Count
        {
            get { return _stack.Count; }
        }

        public int RuleCount
        {
            get { return _rulestack.Count; }
        }

        #endregion

        #region Construction

        protected ReductionStack()
        { }
        
        /// <summary>
        /// Creates a new stack object with a given set of reduction rules.
        /// </summary>
        /// <param name="rules">The set of initial reduction rules.</param>
        public ReductionStack(ReductionRuleCollection rules)
        {
            PushRules(rules);
        }

        #endregion

        #region Reduction Rule Set

        /// <summary>
        /// Returns the current reduction rule set.
        /// </summary>
        public ReductionRuleCollection Top { get { return _rulestack.Count > 0 ? _rulestack.Peek() : null; } }

        /// <summary>
        /// Pushes a set of reduction rules to the rule stack.
        /// </summary>
        /// <param name="rules"></param>
        public virtual void PushRules(ReductionRuleCollection rules)
        {
            if (null == rules) throw new ArgumentNullException("rules");
            _rulestack.Push(rules);
        }

        /// <summary>
        /// Pops reduction rules from the rule stack.
        /// </summary>
        public virtual void PopRules()
        {
            _rulestack.Pop();
        }

        #endregion

        #region Stack Manipulations

        /// <summary>
        /// Adds an item to the right side.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public virtual void Push(object item)
        {
            _stack.Add(item);
        }

        /// <summary>
        /// Performs reduction of the stack with a lookahead.
        /// </summary>
        /// <param name="la">The lookahead symbol.</param>
        public void Reduce(object la)
        {
            var match = true;
            while (_stack.Any() && match)
            {
                BeforeReduce(la);
                match = false;
                foreach (var rule in Top.Select(this, la))
                {
                    if (ApplyRule(rule, la))
                    {
                        match = true;
                        break;
                    }
                }

                if(match && _reducebreak)
                {
                    _reducebreak = false;
                    break;
                }
            }
        }

        /// <summary>
        /// Breaks the current reduction pass.
        /// </summary>
        public void EndReduce()
        {
            _reducebreak = true;
        }

        /// <summary>
        /// Replaces a number of items to the right with a single replacement item.
        /// </summary>
        /// <param name="remove">Number of items to remove from the right.</param>
        /// <param name="replace">Item to append to the right.</param>
        public void Replace(int remove, object replace)
        {
            _stack.RemoveRange(Count - remove, remove);
            _stack.Add(replace);
        }

        /// <summary>
        /// Removes a number of items from the tail of the stack.
        /// </summary>
        /// <param name="remove">The number of items to remove.</param>
        public void PopRight(int remove)
        {
            _stack.RemoveRange(Count - remove, remove);
        }

        /// <summary>
        /// Removes a number of stack elements from the head of the stack.
        /// </summary>
        /// <param name="remove">The number of elements to remove.</param>
        public void PopLeft(int remove)
        {
            _stack.RemoveRange(0, remove);
        }

        /// <summary>
        /// Returns the value of a stack item as a given type.
        /// </summary>
        /// <typeparam name="T">The desired type.</typeparam>
        /// <param name="index">The index of the requested item.</param>
        /// <returns>The object.</returns>
        /// <exception cref="ArgumentException">When the desired type is not compatible with the actual object.</exception>
        public T Get<T>(int index)
        {
            var t = this[index];
            if(t is T)
            {
                return (T)t;
            }
            else
            {
                throw new ArgumentException("requested stack item has incompatible type.");
            }
        }

        /// <summary>
        /// Tries to get a specific stack item as a given type.
        /// </summary>
        /// <typeparam name="T">The desired type.</typeparam>
        /// <param name="index">The index of the requested item.</param>
        /// <param name="t">Receives the result, if successful.</param>
        /// <returns>True if successful.</returns>
        public bool TryGet<T>(int index, out T t)
        {
            var u = this[index];
            if (u is T)
            {
                t = (T)u;
                return true;
            }
            else
            {
                t = default(T);
                return false;
            }

        }

        /// <summary>
        /// Checks if a specific stack item is of given type.
        /// </summary>
        /// <typeparam name="T">The desired type.</typeparam>
        /// <param name="index">The index of the item.</param>
        /// <returns>True if the item type matches.</returns>
        public bool Match<T>(int index)
        {
            return this[index] is T;
        }

        /// <summary>
        /// Matches a sequence of predicates from the right.
        /// </summary>
        /// <param name="pattern">The sequence of predicates in left to right order.</param>
        /// <returns>True if all the predicates match.</returns>
        public bool MatchRight(Predicate[] pattern)
        {
            var length = pattern.Length;
            if (length <= Count)
            {
                var offset = Count - length;
                for (int j = 0; j < length; ++j)
                {
                    if(!pattern[j](_stack[offset + j]))
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Matches a pattern from the left.
        /// </summary>
        /// <param name="pattern">The pattern given as array of predicates.</param>
        /// <returns>True if the pattern matches.</returns>
        public bool MatchLeft(Predicate[] pattern)
        {
            var length = pattern.Length;
            if (length <= Count)
            {
                for (int j = 0; j < length; ++j)
                {
                    if(!pattern[j](_stack[j]))
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public IEnumerable<object> GetRange(int start, int end)
        {
            for (int j = start; j < end; ++j)
            {
                yield return this[j];
            }
        }

        /// <summary>
        /// Clears the stack.
        /// </summary>
        public void Clear()
        {
            _stack.Clear();
        }

        #endregion

        #region Overridables

        /// <summary>
        /// Diagnostic, called before reduction of lookahead is started.
        /// </summary>
        /// <param name="la">The lookahead symbol.</param>
        protected virtual void BeforeReduce(object la)
        {
        }

        /// <summary>
        /// Applies a reduction rule to the stack.
        /// </summary>
        /// <param name="rule">The rule to apply.</param>
        /// <param name="lookahead">The lookahead symbol.</param>
        /// <returns>True if the rule was applied.</returns>
        protected virtual bool ApplyRule(ReductionRule rule, object lookahead)
        {
            return rule.Apply(this, lookahead);
        }

        #endregion

        #region IEnumerable

        /// <summary>
        /// Returns an enumerator for the items on the stack, from left to right.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<object> GetEnumerator()
        {
            return _stack.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _stack.GetEnumerator();
        }

        #endregion
    }
}
