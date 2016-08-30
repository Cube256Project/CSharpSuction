
namespace Common
{
    public enum LogPrinterFormat
    {
        unspecified,
        minimal,
        extended
    }

    public static class LogPrinterFormatExtension
    {
        public static LogPrinterFormat GetEffective(this LogPrinterFormat format)
        {
            if(format != LogPrinterFormat.unspecified)
            {
                return format;
            }
            else if (Log.DefaultFormat != LogPrinterFormat.unspecified)
            {
                return Log.DefaultFormat;
            }
            else
            {
                return LogPrinterFormat.minimal;
            }
        }
    }
}
