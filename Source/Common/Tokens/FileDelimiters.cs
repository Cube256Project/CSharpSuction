namespace Common.Tokens
{
    public abstract class FileDelimiter : Token { }

    public sealed class StartOfFile : FileDelimiter { }

    public sealed class EndOfFile : FileDelimiter { }
}
