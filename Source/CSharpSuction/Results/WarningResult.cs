using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSuction.Results
{
    public abstract class WarningResult
    {
        public ILogMessageTemplate Message { get; private set; }

        public WarningResult(ILogMessageTemplate log)
        {
            Message = log;
        }
    }
}
