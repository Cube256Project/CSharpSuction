using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;  

namespace Common
{
    class BasicLogMessage : ILogMessageTemplate, ILogMessage
    {
        #region Private

        private List<ILogMessageContent> _content = new List<ILogMessageContent>();

        #endregion

        #region Properties

        public string Key { get; private set; }

        public string EventCode { get; set; }

        public DateTime EventTime { get; set; }

        public string Origin { get; set; }

        public LogSeverity Severity { get; set; }

        public string Header { get; set; }

        /// <summary>
        /// Reference to the source code making this log.
        /// </summary>
        public string Source { get; set; }

        public ILogMessageContent[] Body { get { return _content.ToArray(); } }

        public object[] Context { get; set; }

        public long Sequence { get; set; }

        #endregion

        #region Construction

        public BasicLogMessage()
        {
            Severity = LogSeverity.none;
            Context = new object[0];

            // TODO: CTRMIEWANN: this is only one option to do it.
            var id = new byte[32];
            RandomNumberGenerator.Create().GetBytes(id);
            Key = id.ToBase32();

            Origin = "machine:" + Environment.MachineName + ":pid:" + Process.GetCurrentProcess().Id;
        }

        #endregion

        #region Public Methods

        public void AddData(object obj)
        {
            if (null != obj)
            {
                _content.Add(new LogContentConverter().ConvertToLogContent(obj));
            }
        }

        public void AddContext(object obj)
        {
            Context = Context.Concat(new[] { obj }).ToArray();
        }

        public ILogMessage Commit()
        {
            EnsureEventTime();
            return this;
        }

        public void Submit()
        {
            EnsureEventTime();

            // submit to current context.
            LogContext.Current.Submit(this);
        }

        #endregion

        #region Private Methods

        private void EnsureEventTime()
        {
            if (EventTime == default(DateTime))
            {
                // CTRMIEWANN: integration optional?
                EventTime = UniqueTimeSource.GetUniqueTime();
            }
        }

        #endregion
    }
}
