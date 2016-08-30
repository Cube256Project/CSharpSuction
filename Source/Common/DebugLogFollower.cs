using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Common
{
    /// <summary>
    /// Log follower writing pretty into debug output.
    /// </summary>
    public class DebugLogFollower : ILogFollower
    {
        public LogPrinterFormat Format { get; set; }

        public bool IsExclusive { get { return false; } }

        public DebugLogFollower()
        {
        }

        public void Submit(ILogMessage log, LogMessageSubmitOptions options)
        {
            if (options.GetEffective(Format) == LogPrinterFormat.extended)
            {
                var sb = new StringBuilder();
                sb.AppendLine(new string('_', 80));

#if FOO
                if (false)
                {
                    var f = false;
                    foreach (var c in log.Context)
                    {
                        var text = c is Type ? ((Type)c).Name : c.ToString();
                        sb.Append("[" + text + "]");
                        f = true;
                    }
                    if (f)
                    {
                        sb.AppendLine();
                    }
                }
#endif

                sb.AppendLine(log.Header);
                if (null != log.Body)
                {
                    sb.Append(log.Body.ToString());
                }

                Debug.WriteLine(sb.ToString());
            }
            else
            {
                //var line = Thread.CurrentThread.ManagedThreadId + ": " + log.Header;
                var line = log.Header;
                Debug.WriteLine(line);
            }
        }
    }
}
