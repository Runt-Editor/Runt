using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runt.DesignTimeHost.Outgoing
{
    public class InitializeMessage
    {
        public string TargetFramework { get; set; }
        public string ProjectFolder { get; set; }
    }
}
