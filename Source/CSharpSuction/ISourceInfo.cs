using System;
using System.Collections.Generic;

namespace CSharpSuction
{
    /// <summary>
    /// Describes a source file.
    /// </summary>
    public interface ISourceInfo
    {
        /// <summary>
        /// The full path of the source file.
        /// </summary>
        string FullPath { get; }

        /// <summary>
        /// MSBUILD template for the project file.
        /// </summary>
        string Template { get; }

        /// <summary>
        /// The current state of the source file.
        /// </summary>
        SourceState State { get; }

        /// <summary>
        /// Returns a collection of sources this source depends on (MSBUILD-wise).
        /// </summary>
        IEnumerable<ISourceInfo> DependentUpon { get; }

        void Schedule();

        void AddPostProcessing(Action<ISourceInfo> action);

        void AddDependentUpon(ISourceInfo input);
    }
}
