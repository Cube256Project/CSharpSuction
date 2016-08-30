using System;

namespace Common
{
    /// <summary>
    /// Log follower writing pretty to console.
    /// </summary>
    public class ConsoleLogFollower : ILogFollower
    {
        #region Private

        private ConsoleWriter _writer = new ConsoleWriter();

        #endregion

        public LogPrinterFormat Format { get; set; }

        public bool IsExclusive
        {
            get { return false; }
        }

        public ConsoleLogFollower()
        {
        }

        public void Submit(ILogMessage log, LogMessageSubmitOptions options)
        {
            var extended = options.GetEffective(Format) == LogPrinterFormat.extended;

            if (extended)
            {
                _writer.WriteLine(new string('_', 80));
            }

            if (extended)
            {
                _writer.SetColor(ConsoleColor.DarkGray);
                var f = false;
                foreach (var c in log.Context)
                {
                    _writer.Write("[" + c + "]");
                    f = true;
                }

                if (f)
                {
                    _writer.WriteLine();
                }
            }

            _writer.ClearColor();

            if (log.Severity >= LogSeverity.error)
            {
                _writer.SetColor(ConsoleColor.Red);
            }
            else if (log.Severity >= LogSeverity.warning)
            {
                _writer.SetColor(ConsoleColor.Yellow);
            }
            else if (log.Severity >= LogSeverity.success)
            {
                _writer.SetColor(ConsoleColor.Green);
            }
            else if (log.Severity >= LogSeverity.information)
            {
                _writer.SetColor(ConsoleColor.White);
            }
            else
            {
                // default color
            }

            _writer.WriteLine(log.Header);
            _writer.ClearColor();

            if (extended && null != log.Body)
            {
                _writer.WriteLine();
                _writer.Indent();
                //

                foreach (var body in log.Body)
                {
                    var content = body.Content;
                    if (content is string)
                    {
                        _writer.WriteMultiLine((string)content);
                    }
                    else if (content is byte[])
                    {
                        _writer.WriteMultiLine(HexDump.Convert((byte[])content));
                    }
                    else
                    {
                        // TODO: warn?
                    }
                }

                _writer.Unindent();
                _writer.WriteLine();
            }
        }
    }
}
