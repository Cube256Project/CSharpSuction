using System.Collections.Generic;
using System.Linq;

namespace Common
{
    public static class Log
    {
        /// <summary>
        /// Optional factory for log messages.
        /// </summary>
        public static ILogMessageFactory Factory { get; set; }

        public static LogPrinterFormat DefaultFormat { get; set; }

        public static LogSeverity DefaultMinimumSeverity { get; set; }

        public static LogSeverity ClassTraceSeverity { get; set; }

        static Log()
        {
            ClassTraceSeverity = LogSeverity.debug;
        }

        /// <summary>
        /// Creates a new, empty log message.
        /// </summary>
        /// <returns>The log message.</returns>
        /// <remarks>After filling properties, the message must be submitted.</remarks>
        public static ILogMessageTemplate Create()
        {
            var log = null == Factory ? new BasicLogMessage() : Factory.CreateMessage();
            log.SerializeContext(LogContext.Context);
            return log;
        }

        public static ILogMessageTemplate CreateDebug()
        {
            return Create().WithSeverity(LogSeverity.debug);
        }

        public static ILogMessageTemplate CreateWarning()
        {
            return Create().WithSeverity(LogSeverity.warning);
        }

        public static ILogMessageTemplate CreateError()
        {
            return Create().WithSeverity(LogSeverity.error);
        }

        public static ILogMessageTemplate CreateInformation()
        {
            return Create().WithSeverity(LogSeverity.information);
        }

        public static void Trace(string format, params object[] args)
        {
            var log = Create();
            log.Header = string.Format(format, args);
            log.Submit();
        }

        public static void Debug(string format, params object[] args)
        {
            var log = Create();
            log.Severity = LogSeverity.debug;
            log.Header = string.Format(format, args);
            log.Submit();
        }

        public static void Information(string format, params object[] args)
        {
            var log = Create();
            log.Severity = LogSeverity.information;
            log.Header = string.Format(format, args);
            log.Submit();
        }

        public static void Warning(string format, params object[] args)
        {
            var log = Create();
            log.Severity = LogSeverity.warning;
            log.Header = string.Format(format, args);
            log.Submit();
        }

        public static void Error(string format, params object[] args)
        {
            var log = Create();
            log.Severity = LogSeverity.error;
            log.Header = string.Format(format, args);
            log.Submit();
        }
    }
}
