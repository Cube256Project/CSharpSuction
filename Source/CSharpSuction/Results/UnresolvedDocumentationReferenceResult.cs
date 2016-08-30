using Common;

namespace CSharpSuction.Results
{
    class UnresolvedDocumentationReferenceResult : UnresolvedReferenceResult
    {
        public UnresolvedDocumentationReferenceResult(string cref, ILogMessageTemplate log) 
            : base(cref, log)
        {
        }
    }
}
