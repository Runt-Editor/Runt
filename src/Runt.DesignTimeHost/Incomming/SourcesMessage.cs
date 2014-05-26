using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runt.DesignTimeHost.Incomming
{
    public class SourcesMessage
    {
        public IList<string> Files { get; set; }
        public IDictionary<string, string> GeneratedFiles { get; set; }
    }
}
