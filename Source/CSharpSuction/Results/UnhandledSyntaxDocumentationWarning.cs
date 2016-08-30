using Common;

namespace CSharpSuction.Results
{
    class UnhandledSyntaxDocumentationWarning : WarningResult
    {
        public UnhandledSyntaxDocumentationWarning(string message) 
            : base(Log.CreateWarning().WithHeader("{0}", message))
        {
        }
    }
}
