using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSuction.Generation
{
    sealed class StateSection : IDisposable
    {
        private int _count;
        private bool _disposed;
        private EmitTypeScript _parent;

        public StateSection(EmitTypeScript parent, params ScriptStateModifier[] mods)
        {
            _parent = parent;

            foreach (var mod in mods)
            {
                _parent.Push(mod);
            }

            _count = mods.Length;
        }

        public void Dispose()
        {
            if(!_disposed)
            {
                _disposed = true;
                _parent.Pop(_count);
            }
        }
    }
}
