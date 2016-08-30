using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Linq;

namespace Common
{
    /// <summary>
    /// Log context holding thread specific variables. 
    /// </summary>
    public class LogContext : IDisposable
    {
        #region Private

        private static ThreadLocal<LogContext> _current = new ThreadLocal<LogContext>(() => Default);
        private LogContext _previous;
        private object[] _arguments;
        private HashSet<ILogFollower> _followers;

        #endregion

        #region Properties

        /// <summary>
        /// Global log context.
        /// </summary>
        public static readonly LogContext Default = new LogContext(true);

        /// <summary>
        /// The current log context.
        /// </summary>
        public static LogContext Current { get { return _current.Value; } }

        public static IEnumerable<object> Context
        {
            get
            {
                var c = _current.Value;
                while (null != c)
                {
                    if (null != c._arguments)
                    {
                        foreach (var arg in c._arguments)
                        {
                            yield return arg;
                        }
                    }

                    c = c._previous;
                }
            }
        }

        /// <summary>
        /// The minimum severity for log messages to be dispatched from this context.
        /// </summary>
        public LogSeverity MinimumSeverity { get; set; }

        public LogPrinterFormat Format { get; set; }

        #endregion

        #region Construction

        private LogContext(bool root)
        {
            OnCreateFollowers(root);
        }

        /// <summary>
        /// Creates a new log context.
        /// </summary>
        /// <param name="context">Optional context object.</param>
        public LogContext(params object[] context)
            : this(false)
        {
            _previous = _current.Value;

            // at least the default is always there
            Debug.Assert(null != _previous);

            // push onto context stack
            _current.Value = this;

            // specifier
            _arguments = context;

            // see if trace enabled for any of the arguments ...
            if (_arguments.Any(e => IsTraceEnabled(e)))
            {
                // lower minimum log severity for enabled classes.
                MinimumSeverity = LogSeverity.none;
            }
            else
            {
                // go with default
                MinimumSeverity = LogSeverity.unspecified;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (null != _previous)
            {
                _current.Value = _previous;
                _previous = null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a follower to this log context.
        /// </summary>
        /// <param name="follower">The follower object, can be null.</param>
        public void AddFollower(ILogFollower follower)
        {
            if (null == follower)
            {
                return;
            }

            if (!ContainsFollower(follower))
            {
                if (null == _followers)
                {
                    _followers = new HashSet<ILogFollower>();
                }

                _followers.Add(follower);
            }
        }

        public bool ContainsFollower(ILogFollower follower)
        {
            if (null != _followers && _followers.Contains(follower))
            {
                return true;
            }

            if (null != _previous)
            {
                return _previous.ContainsFollower(follower);
            }

            return false;
        }

        /// <summary>
        /// Removes a follower from this log context.
        /// </summary>
        /// <param name="follower"></param>
        public void RemoveFollower(ILogFollower follower)
        {
            if (null != _followers)
            {
                _followers.Remove(follower);
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Submits a message into this log context and its followers.
        /// </summary>
        /// <param name="log">The message to submit.</param>
        internal void Submit(ILogMessage log, LogMessageSubmitOptions options = null)
        {
            options = null != options ? options.Clone() : new LogMessageSubmitOptions();

            var pass = true;
            if (!options.NoFilter)
            {
                // filter severity 
                var minimum = MinimumSeverity.GetEffective();
                pass = minimum <= log.Severity;

                if (minimum != LogSeverity.unspecified)
                {
                    // don't filter again at lower levels.
                    options.NoFilter = true;
                }
            }

            if (pass)
            {
                // adjust format.
                if (options.Format == LogPrinterFormat.unspecified)
                {
                    options.Format = Format;
                }

                if (null != _followers)
                {
                    foreach (var follower in _followers)
                    {
                        follower.Submit(log, options);

                        if (follower.IsExclusive)
                        {
                            // stop forwarding to containing followers.
                            return;
                        }
                    }
                }

                if (null != _previous)
                {
                    // pass down, no more filtering.
                    _previous.Submit(log, options);
                }
            }
            else
            {
                // discard
            }
        }

        #endregion

        #region Overrideables

        protected virtual void OnCreateFollowers(bool root)
        {
            if (root)
            {
                AddFollower(new DebugLogFollower());
            }
        }

        #endregion

        #region Private Methods

        private bool IsTraceEnabled(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            else if (obj is Type)
            {
                return ((Type)obj).IsTraceEnabled();
            }
            else
            {
                return obj.GetType().IsTraceEnabled();
            }
        }

        #endregion
    }
}
