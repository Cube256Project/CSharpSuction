using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Common
{
    public static class LogMessageExtensions
    {
        public static ILogMessageTemplate WithHeader(this ILogMessageTemplate log, string format, params object[] args)
        {
            log.Header = string.Format(format, args);
            return log;
        }

        public static ILogMessageTemplate WithData(this ILogMessageTemplate log, object data)
        {
            log.AddData(data);
            return log;
        }

        public static ILogMessageTemplate WithEventCode(this ILogMessageTemplate log, object context, string eventcode)
        {
            log.EventCode = context.GetType().FullName + "." + eventcode;
            return log;
        }

        public static ILogMessageTemplate WithEventCode(this ILogMessageTemplate log, string eventcode)
        {
            log.EventCode = eventcode;
            return log;
        }

        public static ILogMessageTemplate WithSeverity(this ILogMessageTemplate log, LogSeverity severity)
        {
            log.Severity = severity;
            return log;
        }

        public static ILogMessageTemplate WithExceptionData(this ILogMessageTemplate log, Exception ex)
        {
            var first = true;
            var sb = new StringBuilder();
            while (null != ex)
            {
                if (!first) sb.AppendLine("-------------");

                sb.AppendLine("[" + ex.GetType().FullName + "] " + ex.Message);

                if(first) sb.AppendLine(ex.ToString());

                first = false;
                ex = ex.InnerException;
            }

            log.AddData(sb.ToString());

            return log;
        }

        public static ILogMessageTemplate WithSource(this ILogMessageTemplate log, [CallerFilePath] string filename = null, [CallerLineNumber] int line = 0)
        {
            log.Source = filename + "(" + line + ")";
            return log;
        }

        internal static void SerializeContext(this ILogMessageTemplate log)
        {
        }

        internal static void SerializeContext(this ILogMessageTemplate log, IEnumerable<object> context)
        {
            var set = new HashSet<object>();
            var list = new List<string>();

            foreach (var e in context)
            {
                if (null != e && !set.Contains(e))
                {
                    set.Add(e);
                    list.Add(ConvertToString(e));
                }
            }

            log.Context = list.ToArray();
        }

        private static string ConvertToString(object e)
        {
            if (e is string)
            {
                return (string)e;
            }
            else
            {
                var type = e.GetType();
                var source = type.GetCustomAttribute<LogSourceAttribute>(true);
                if (null != source)
                {
                    try
                    {
                        // get context property from object given by dotted propertypath.
                        var parts = source.PropertyPath.Split('.');
                        foreach (var part in parts)
                        {
                            var prop = type.GetProperty(part);
                            if (null == prop)
                                break;

                            e = prop.GetValue(e);
                            if (null == e)
                                break;

                            type = e.GetType();
                        }

                        return e.ToString();
                    }
                    catch (Exception)
                    { 
                    }
                }

                return type.FullName;
            }
        }
    }
}
