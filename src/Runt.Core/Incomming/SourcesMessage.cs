using System.Collections.Generic;

namespace Runt.DesignTimeHost.Incomming
{
    public class SourcesMessage
    {
        public IList<string> Files { get; set; }
        public IDictionary<string, string> GeneratedFiles { get; set; }
    }
}
