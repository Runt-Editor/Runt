using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runt.DesignTimeHost
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
