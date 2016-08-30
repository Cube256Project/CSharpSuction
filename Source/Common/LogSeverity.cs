
namespace Common
{
    public enum LogSeverity
    {
        unspecified = 0,
        /// <summary>
        /// The lowest possible log level; encompasses all messages.
        /// </summary>
        none = 1,
        output = 50,
        echo = 70,
        debug = 100,
        security = 160,
        information = 200,
        success = 250,
        warning = 300,
        error = 400,

        never = 1000
    }

    public static class LogSeverityExtension
    {
        public static bool IsIncluded(this LogSeverity severity, LogSeverity rule)
        {
            // default handling
            if (rule == LogSeverity.unspecified)
            {
                // use context setting
                rule = LogContext.Current.MinimumSeverity;
                if (rule == LogSeverity.unspecified)
                {
                    // use global setting
                    rule = Log.DefaultMinimumSeverity;
                }
            }

            return severity >= rule;
        }

        public static LogSeverity GetEffective(this LogSeverity severity)
        {
            if(severity == LogSeverity.unspecified)
            {
                return Log.DefaultMinimumSeverity;
            }
            else
            {
                return severity;
            }
        }
    }
}
