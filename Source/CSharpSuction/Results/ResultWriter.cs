using Common;
using Common.Tokenization;

namespace CSharpSuction.Results
{
    public class ResultWriter : TokenWriter
    {
        protected override void AppendLine()
        {
            Log.Debug("{0}", Text);
            Clear();
        }

        public void Debug(string format, params object[] args)
        {
            WriteLine(string.Format(format, args));
        }

        public void Write(WarningResult warning)
        {
            if (!IsSuppressed(warning))
            {
                var log = warning.Message;

                log.Submit();
            }
        }

        private bool IsSuppressed(object argument)
        {
            if (argument is UnresolvedDocumentationReferenceResult)
            {
                // return true;
                return ((UnresolvedDocumentationReferenceResult)argument).Message.Severity <= LogSeverity.debug;
            }
            else if (argument is UnprocessedFileWarning)
            {
                return true;
            }

            return false;
        }
    }
}
