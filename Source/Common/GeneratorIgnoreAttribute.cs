using System;

namespace Common
{
    /// <summary>
    /// Marks as ignorable by a code generator.
    /// </summary>
    [GeneratorIgnore]
    [AttributeUsage(AttributeTargets.All)]
    class GeneratorIgnoreAttribute : Attribute
    {
    }
}
