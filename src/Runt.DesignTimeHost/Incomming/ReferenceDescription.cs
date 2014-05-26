using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runt.DesignTimeHost.Incomming
{
    public class ReferenceDescription
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Path { get; set; }
        public string Type { get; set; }
        public IEnumerable<ReferenceItem> Dependencies { get; set; }
    }
}
