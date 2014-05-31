using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runt.DesignTimeHost.Incomming
{
    public class DiagnosticsMessage
    {
        public IList<string> Warnings { get; set; }
        public IList<string> Errors { get; set; }
    }
}
