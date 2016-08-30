using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSuction.Results
{
    abstract class UnresolvedReferenceResult : WarningResult
    {
        public string Reference { get; private set; }

        public UnresolvedReferenceResult(string cref, ILogMessageTemplate log) : base(log)
        {
            Reference = cref;
        }
    }
}
