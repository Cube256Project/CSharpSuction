namespace CSharpSuction.Input
{
    class EmbeddedResourceSourceInfo : SourceInfo
    {
        public EmbeddedResourceSourceInfo(Suction suction, string fullpath)
            : base(suction, fullpath)
        {
            SetState(SourceState.EmbeddedResource);
        }
    }
}
