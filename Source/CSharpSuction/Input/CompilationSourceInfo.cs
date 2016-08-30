using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSuction.Input
{
    class CompilationSourceInfo : SourceInfo
    {
        public CompilationSourceInfo(Suction suction, string fullpath) 
            : base(suction, fullpath)
        { }
    }
}
