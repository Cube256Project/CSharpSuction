using System;
using System.Collections.Generic;

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

        public Dictionary<string, string> Parameters = new Dictionary<string, string>();

        public EmitInstruction()
        { }

        public EmitInstruction(Type type)
        {
            EmitterType = type;
        }
    }
}
