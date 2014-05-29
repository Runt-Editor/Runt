using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runt.Core
{
    public interface IClientConnection
    {
        Task Send(string message);
    }
}
