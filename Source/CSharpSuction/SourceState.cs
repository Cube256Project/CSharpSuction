namespace CSharpSuction
{
    /// <summary>
    /// Describes the state of a source file included in the suction.
    /// </summary>
    public enum SourceState
    {
        Initial,
        Extracted,
        Scheduled,
        Resolved,
        Extended,
        Unresolved,
        Resolving,
        EmbeddedResource
    }

    public static class SourceStateExtensions
    {
        public static bool IsIncluded(this SourceState s)
        {
            switch(s)
            {
                case SourceState.Resolved:
                case SourceState.EmbeddedResource:
                    return true;

                default:
                    return false;
            }
        }
    }
}
