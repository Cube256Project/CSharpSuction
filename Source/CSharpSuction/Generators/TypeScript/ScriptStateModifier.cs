using Common.Tokenization;

namespace CSharpSuction.Generators.TypeScript
{
    /// <summary>
    /// Elements of the execution modification stack.
    /// </summary>
    abstract class ScriptStateModifier
    {
    }

    class ThisRedirectModifier : ScriptStateModifier
    {
        public string ThisName { get; private set; }

        public ThisRedirectModifier(string altname)
        {
            ThisName = altname;
        }
    }

    class WriterRedirectModifier : ScriptStateModifier
    {
        public TokenWriter Writer { get; private set; }

        public WriterRedirectModifier(TokenWriter writer)
        {
            Writer = writer;
        }
    }

    class EventAssignmentModifier : ScriptStateModifier
    { }

    class InitializerModifier : ThisRedirectModifier
    {
        public string AlternativeLeft { get; private set; }

        public InitializerModifier(string left, string right) : base(right)
        {
            AlternativeLeft = left;
        }
    }
}
