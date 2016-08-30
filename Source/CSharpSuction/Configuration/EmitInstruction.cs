using System;

namespace CSharpSuction.Configuration
{
    /// <summary>
    /// Parameters for <see cref="Emit"/>.
    /// </summary>
    public class EmitInstruction
    {
        public string Kind;

        public string Destination;

        public Type EmitterType;

        public EmitInstruction()
        { }

        public EmitInstruction(Type type)
        {
            EmitterType = type;
        }
    }
}
