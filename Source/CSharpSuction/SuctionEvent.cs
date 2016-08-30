using System;
using System.Collections.Generic;

namespace CSharpSuction
{
    public enum SuctionEventCode
    {
        None,
        ExtractUsing,
        IncludeUsing,
        AmbigousFactory,
        CompilationError,
        Message,
        SourceLoaded,
        ResolverCreated,
        UnprocessedFile
    }

    public class SuctionEventArgs : EventArgs
    {
        public SuctionEventCode Code { get; set; }

        public string Name { get; set; }

        public IEnumerable<FactoryChoice> Mapping { get; set; }

        public string Message { get; set; }

        public string FullPath { get; set; }

        public ISourceInfo Source { get; set; }

        public bool Handled { get; set; }
    }

    public delegate void SuctionEventHandler(object sender, SuctionEventArgs e);
}
