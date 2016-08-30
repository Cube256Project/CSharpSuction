using System.IO;

namespace Common
{
    public abstract class LogMessageContent : ILogMessageContent
    {
        public string ContentType { get; private set; }

        public abstract object Content { get; }

        protected LogMessageContent(string contenttype)
        {
            ContentType = contenttype;
        }
    }

    public class LogMessageTextContent : LogMessageContent
    {
        public string Text { get; private set; }

        public override object Content { get { return Text; } }

        public LogMessageTextContent(string text, string contenttype = "text/plain")
            : base(contenttype)
        {
            Text = text;
        }
    }

    public class LogMessageBinaryContent : LogMessageContent
    {
        public byte[] Data { get; private set; }

        public override object Content { get { return Data; } }

        public LogMessageBinaryContent(Stream content, string contenttype = "application/octet-stream")
            : base(contenttype)
        {
            var ms = new MemoryStream();
            content.CopyTo(ms);
            Data = ms.ToArray();
        }

        public LogMessageBinaryContent(byte[] data, string contenttype = "application/octet-stream")
            : base(contenttype)
        {
            Data = data;
        }

    }
}
