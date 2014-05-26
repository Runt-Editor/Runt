using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Runt.DesignTimeHost.Incomming;

namespace Runt.DesignTimeHost
{
    public class ReferencesEventArgs : ProjectEventArgs
    {
        readonly ReferencesMessage _message;

        public ReferencesEventArgs(int contextId, ReferencesMessage message)
            : base(contextId)
        {
            _message = message;
        }

        
    }
}
