using System;

namespace Common
{
    [AttributeUsage(AttributeTargets.Class)]
    public class LogSourceAttribute : Attribute
    {
        public string PropertyPath { get; private set; }

        public LogSourceAttribute(string keyprop)
        {
            PropertyPath = keyprop;
        }
    }
}
