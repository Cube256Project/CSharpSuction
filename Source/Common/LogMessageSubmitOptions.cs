
namespace Common
{
    public class LogMessageSubmitOptions
    {
        public LogPrinterFormat Format;

        public bool NoFilter;

        internal LogPrinterFormat GetEffective(LogPrinterFormat format)
        {
            if(format == LogPrinterFormat.unspecified)
            {
                if (format == LogPrinterFormat.unspecified)
                {
                    return Log.DefaultFormat;
                }
                else
                {
                    return Format;
                }
            }
            else
            {
                return format;
            }
        }

        internal LogMessageSubmitOptions Clone()
        {
            return (LogMessageSubmitOptions)MemberwiseClone();
        }
    }
}
