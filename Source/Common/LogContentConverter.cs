using System.Text;

namespace Common
{
    /// <summary>
    /// X6F24OPYV5: converts log argument content into <see cref="ILogMessageContent"/>.
    /// </summary>
    public class LogContentConverter
    {
        public ILogMessageContent ConvertToLogContent(object obj)
        {
            if (obj is string)
            {
                return new LogMessageTextContent((string)obj);
            }
            else if (obj is byte[])
            {
                return new LogMessageBinaryContent((byte[])obj);
            }
            else if (obj is StringBuilder)
            {
                return new LogMessageTextContent(((StringBuilder)obj).ToString());
            }
            else
            {
                return new LogMessageTextContent("[" + obj.GetType().AssemblyQualifiedName + "]");
            }
        }
    }
}
