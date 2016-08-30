using System;
using Common;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSuction.Results
{
    class UnprocessedFileWarning : WarningResult
    {
        public UnprocessedFileWarning(string sourcefile) 
            : base(Log.CreateWarning().WithHeader("source file {0} was not processed.", sourcefile.Quote()))
        {
        }
    }
}
