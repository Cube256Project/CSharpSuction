
using System;

namespace Common
{
    public interface ILogMessageContent
    {
        string ContentType { get; }

        object Content { get; }
    }

    public interface ILogMessageIdentification
    {
        /// <summary>
        /// Unique per message identification.
        /// </summary>
        string Key { get; }
    }

    /// <summary>
    /// Readonly representation of log message.
    /// </summary>
    public interface ILogMessage : ILogMessageIdentification
    {
        /// <summary>
        /// URI describing the event.
        /// </summary>
        string EventCode { get; }

        DateTime EventTime { get; }

        /// <summary>
        /// A string describing the origin of the message.
        /// </summary>
        string Origin { get; }

        /// <summary>
        /// Severity must be at least 'none'.
        /// </summary>
        LogSeverity Severity { get; }

        /// <summary>
        /// The log message header.
        /// </summary>
        string Header { get; }

        string Source { get; }

        /// <summary>
        /// The body of the log message.
        /// </summary>
        ILogMessageContent[] Body { get; }

        object[] Context { get; }

        /// <summary>
        /// Assigned by log operators.
        /// </summary>
        long Sequence { get; set; }
    }

    /// <summary>
    /// Interface provided by modifiable message templates.
    /// </summary>
    public interface ILogMessageTemplate : ILogMessageIdentification
    {
        /// <summary>
        /// URI describing the event.
        /// </summary>
        string EventCode { get; set; }

        DateTime EventTime { get; set; }

        string Origin { get; set; }

        /// <summary>
        /// Severity must be at least 'none'.
        /// </summary>
        LogSeverity Severity { get; set; }

        /// <summary>
        /// The log message header.
        /// </summary>
        string Header { get; set; }

        string Source { get; set; }

        /// <summary>
        /// The body of the log message.
        /// </summary>
        ILogMessageContent[] Body { get; }

        object[] Context { get; set; }

        /// <summary>
        /// Adds a data object to the log message.
        /// </summary>
        /// <remarks>
        /// <para>The object can be a string, StringBuilder or a binary buffer (X6F24OPYV5) via <see cref="LogContentConverter"/>.</para>
        /// </remarks>
        /// <param name="data">The data object to log.</param>
        void AddData(object data);

        /// <summary>
        /// Adds a context object to the message.
        /// </summary>
        /// <param name="obj"></param>
        void AddContext(object obj);

        ILogMessage Commit();

        /// <summary>
        /// Submits the message to the default destination.
        /// </summary>
        void Submit();
    }
}
