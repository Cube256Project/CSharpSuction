using System;

namespace CSharpSuction.Generators.TypeScript
{
    sealed class StateSection : IDisposable
    {
        #region Private

        private int _count;
        private bool _disposed;
        private EmitTypeScript _parent;

        #endregion

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
