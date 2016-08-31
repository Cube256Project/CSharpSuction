using Common;
using System;

namespace CSharpSuction.Results
{
    class TopicReferenceTranslationError : UnresolvedReferenceResult
    {
        public TopicReferenceTranslationError(string cref, Exception ex)
            : base(cref, Log
                  .CreateError()
                  .WithHeader("failed to translate {0}: {1}", cref.Quote(), ex.Message)
                  .WithExceptionData(ex))
        {
        }
    }
}
