using System;

namespace Runt.Core
{
    public abstract class ProjectEventArgs : EventArgs
    {
        readonly int _contextId;

        public ProjectEventArgs(int contextId)
        {
            _contextId = contextId;
        }

        public int ContextId
        {
            get { return _contextId; }
        }
    }
}
