using Common;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;

namespace CSharpSuction.Input
{
    /// <summary>
    /// Represents the state of a single source file included into the suction.
    /// </summary>
    abstract class SourceInfo : ISourceInfo
    {
        #region Private

        private SyntaxTree _tree;
        private Suction _suction;
        private HashSet<ISourceInfo> _references = new HashSet<ISourceInfo>();
        private List<ISourceInfo> _dependentupon = new List<ISourceInfo>();
        private List<Action<ISourceInfo>> _postactions = new List<Action<ISourceInfo>>();

        #endregion

        #region Properties

        public string Name {  get { return Path.GetFileName(FullPath); } }

        /// <summary>
        /// MSBUILD template for the project file.
        /// </summary>
        public virtual string Template { get { return null; } }

        public string FullPath { get; private set; }

        public Checksum Digest { get; private set; }

        public SyntaxTree Tree
        {
            get { return _tree; }
            set
            {
                if (State == SourceState.Initial)
                {
                    _tree = value;
                    SetState(SourceState.Extracted);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public SourceState State { get; private set; }

        public IEnumerable<ISourceInfo> DependentUpon { get { return _dependentupon; } }

        #endregion

        #region Construction

        public SourceInfo(Suction suction, string fullpath)
        {
            _suction = suction;

            State = SourceState.Initial;
            FullPath = fullpath;
            Digest = new Checksum(fullpath);
        }

        #endregion

        #region Public Methods

        public void Schedule()
        {
            if(State == SourceState.Extracted)
            {
                SetState(SourceState.Scheduled);
            }
        }

        public void AddPostProcessing(Action<ISourceInfo> action)
        {
            _postactions.Add(action);
        }

        public void AddDependentUpon(ISourceInfo input)
        {
            _dependentupon.Add(input);
        }

        #endregion

        #region Internal Methods

        internal void Resolving()
        {
            if(State == SourceState.Scheduled)
            {
                SetState(SourceState.Resolving);
            }
        }

        internal void Resolved()
        {
            switch (State)
            {
                case SourceState.Scheduled:
                case SourceState.Resolving:
                    SetState(SourceState.Resolved);
                    break;
            }
        }

        internal void Unresolved()
        {
            switch (State)
            {
                case SourceState.Scheduled:
                case SourceState.Resolving:
                    SetState(SourceState.Unresolved);
                    break;
            }
        }

        internal void AddReference(ISourceInfo othersource)
        {
            if(!_references.Contains(othersource))
            {
                _references.Add(othersource);
            }
        }

        #endregion

        protected void SetState(SourceState newstate)
        {
            if (State != newstate)
            {
                // Log.Trace("{0,-30} {1} -> {2}", Name, State, newstate);
                State = newstate;

                switch (State)
                {
                    case SourceState.Scheduled:
                        _suction.ScheduleCompilation(this);
                        break;

                    case SourceState.Resolved:
                        TriggerPostActions();
                        break;
                }
            }
        }

        #region Private Methods

        private void TriggerPostActions()
        {
            foreach(var action in _postactions)
            {
                action(this);
            }
        }

        #endregion
    }
}
