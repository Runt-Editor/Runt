using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runt.DesignTimeHost.Incomming
{
    public class ReferencesMessage
    {
        public string RootDependency { get; set; }
        public string LongFrameworkName { get; set; }
        public string FriendlyFrameworkName { get; set; }
        public IList<string> ProjectReferences { get; set; }
        public IList<string> FileReferences { get; set; }
        public IDictionary<string, byte[]> RawReferences { get; set; }
        public IDictionary<string, ReferenceDescription> Dependencies { get; set; }
    }
}
