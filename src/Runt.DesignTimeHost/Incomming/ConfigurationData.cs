using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runt.DesignTimeHost.Incomming
{
    public class ConfigurationData
    {
        public string FrameworkName { get; set; }
        public string LongFrameworkName { get; set; }
        public string FriendlyFrameworkName { get; set; }
        public CompilationSettings CompilationSettings { get; set; }
    }
}
