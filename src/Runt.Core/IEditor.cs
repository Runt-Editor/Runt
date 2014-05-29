using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runt.Core
{
    public interface IEditor
    {
        IClientConnection ClientConnection { get; set; }

        void NotifyConnected();
        void NotifyReceived(string data);
    }
}
