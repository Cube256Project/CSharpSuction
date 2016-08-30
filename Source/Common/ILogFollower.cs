
using System;

namespace Common
{
    public interface ILogFollower
    {
        bool IsExclusive { get; }

        void Submit(ILogMessage log, LogMessageSubmitOptions options);
    }
}
